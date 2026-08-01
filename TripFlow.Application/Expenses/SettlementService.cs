using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity;
using TripFlow.Application.Common;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Notifications;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Expenses;

/// <summary>
/// Fecha o ciclo social do settlement calculado pelo SettlementCalculator: o devedor marca
/// uma transferencia sugerida como paga (fora do app - Pix, dinheiro...) e o credor confirma
/// que recebeu. So depois da confirmacao o debito de fato some do saldo (ver
/// SettlementCalculator.CalculateBalances). Sem gateway de pagamento real por tras -
/// honestidade mutua, com notificacao pros dois lados.
/// </summary>
public class SettlementService
{
    private readonly IAppDbContext _db;
    private readonly ITripNotifier _tripNotifier;
    private readonly NotificationService _notificationService;
    private readonly ActivityService _activityService;

    public SettlementService(IAppDbContext db, ITripNotifier tripNotifier, NotificationService notificationService, ActivityService activityService)
    {
        _db = db;
        _tripNotifier = tripNotifier;
        _notificationService = notificationService;
        _activityService = activityService;
    }

    public async Task<ServiceResult<SettlementRecordDto>> MarkAsPaidAsync(Guid tripId, Guid actorUserId, MarkSettlementPaidRequest request, CancellationToken ct = default)
    {
        var fromParticipant = await _db.TripParticipants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TripId == tripId && p.UserId == actorUserId, ct);
        if (fromParticipant is null)
            return ServiceResult<SettlementRecordDto>.Failure(ServiceErrorType.Validation, "Voce precisa ser participante da viagem.");

        if (request.ToParticipantId == fromParticipant.Id)
            return ServiceResult<SettlementRecordDto>.Failure(ServiceErrorType.Validation, "Voce nao pode quitar uma divida com voce mesmo.");

        var toParticipant = await _db.TripParticipants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ToParticipantId && p.TripId == tripId, ct);
        if (toParticipant is null)
            return ServiceResult<SettlementRecordDto>.Failure(ServiceErrorType.Validation, "Participante nao encontrado.");

        var alreadyPending = await _db.SettlementRecords.AnyAsync(s =>
            s.TripId == tripId && s.FromParticipantId == fromParticipant.Id && s.ToParticipantId == request.ToParticipantId
            && s.Status == SettlementRecordStatus.Pending, ct);
        if (alreadyPending)
            return ServiceResult<SettlementRecordDto>.Failure(ServiceErrorType.Conflict, "Ja existe uma quitacao aguardando confirmacao pra esse pagamento.");

        var record = new SettlementRecord
        {
            TripId = tripId,
            FromParticipantId = fromParticipant.Id,
            ToParticipantId = request.ToParticipantId,
            Amount = request.Amount
        };

        _db.SettlementRecords.Add(record);
        await _db.SaveChangesAsync(ct);

        await _tripNotifier.NotifySettlementChangedAsync(tripId, ct);

        var currency = await GetCurrencyAsync(tripId, ct);
        var actorName = fromParticipant.DisplayName ?? fromParticipant.InvitedEmail;
        var toName = toParticipant.DisplayName ?? toParticipant.InvitedEmail;
        var amountText = request.Amount.ToString("0.00", CultureInfo.InvariantCulture);

        if (toParticipant.UserId is { } toUserId)
            await _notificationService.NotifyAsync(tripId, toUserId, NotificationType.SettlementMarkedPaid,
                $"{actorName} marcou como paga uma divida de {amountText} {currency}. Confirme se recebeu.", ct);

        await _activityService.RecordAsync(tripId, fromParticipant.Id, actorUserId, ActivityType.SettlementMarkedPaid, nameof(SettlementRecord), record.Id,
            $"{actorName} marcou como paga uma divida de {amountText} {currency} pra {toName}.", ct);

        return ServiceResult<SettlementRecordDto>.Success(ToDto(record));
    }

    public async Task<ServiceResult<SettlementRecordDto>> ConfirmAsync(Guid tripId, Guid actorUserId, Guid recordId, CancellationToken ct = default)
    {
        var record = await _db.SettlementRecords.FirstOrDefaultAsync(s => s.Id == recordId && s.TripId == tripId, ct);
        if (record is null)
            return ServiceResult<SettlementRecordDto>.Failure(ServiceErrorType.NotFound, "Quitacao nao encontrada.");

        if (record.Status == SettlementRecordStatus.Confirmed)
            return ServiceResult<SettlementRecordDto>.Failure(ServiceErrorType.Conflict, "Essa quitacao ja foi confirmada.");

        var toParticipant = await _db.TripParticipants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == record.ToParticipantId && p.TripId == tripId, ct);
        if (toParticipant is null || toParticipant.UserId != actorUserId)
            return ServiceResult<SettlementRecordDto>.Failure(ServiceErrorType.Validation, "So quem recebeu o pagamento pode confirmar.");

        record.Status = SettlementRecordStatus.Confirmed;
        record.ConfirmedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _tripNotifier.NotifySettlementChangedAsync(tripId, ct);

        var fromParticipant = await _db.TripParticipants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == record.FromParticipantId, ct);
        var currency = await GetCurrencyAsync(tripId, ct);
        var actorName = toParticipant.DisplayName ?? toParticipant.InvitedEmail;
        var fromName = fromParticipant?.DisplayName ?? fromParticipant?.InvitedEmail ?? "alguem";
        var amountText = record.Amount.ToString("0.00", CultureInfo.InvariantCulture);

        if (fromParticipant?.UserId is { } fromUserId)
            await _notificationService.NotifyAsync(tripId, fromUserId, NotificationType.SettlementConfirmed,
                $"{actorName} confirmou o recebimento de {amountText} {currency}.", ct);

        await _activityService.RecordAsync(tripId, record.ToParticipantId, actorUserId, ActivityType.SettlementConfirmed, nameof(SettlementRecord), record.Id,
            $"{actorName} confirmou que recebeu {amountText} {currency} de {fromName}.", ct);

        return ServiceResult<SettlementRecordDto>.Success(ToDto(record));
    }

    private async Task<string> GetCurrencyAsync(Guid tripId, CancellationToken ct) =>
        await _db.Trips.AsNoTracking().Where(t => t.Id == tripId).Select(t => t.Currency).FirstOrDefaultAsync(ct) ?? "BRL";

    private static SettlementRecordDto ToDto(SettlementRecord r) =>
        new(r.Id, r.FromParticipantId, r.ToParticipantId, r.Amount, r.Status, r.CreatedAt, r.ConfirmedAt);
}
