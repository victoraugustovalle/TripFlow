using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity;
using TripFlow.Application.Common;
using TripFlow.Application.Notifications;
using TripFlow.Application.Participants.DTOs;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Participants;

public class ParticipantService
{
    private const int InviteExpiryDays = 7;

    private readonly IAppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ParticipantService> _logger;
    private readonly NotificationService _notificationService;
    private readonly ITripNotifier _tripNotifier;
    private readonly ActivityService _activityService;

    public ParticipantService(
        IAppDbContext db, IEmailSender emailSender, ILogger<ParticipantService> logger,
        NotificationService notificationService, ITripNotifier tripNotifier, ActivityService activityService)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
        _notificationService = notificationService;
        _tripNotifier = tripNotifier;
        _activityService = activityService;
    }

    public async Task<ServiceResult<ParticipantDto>> InviteAsync(Guid tripId, InviteParticipantRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var alreadyInvited = await _db.TripParticipants.AnyAsync(p => p.TripId == tripId && p.InvitedEmail == normalizedEmail, ct);
        if (alreadyInvited)
            return ServiceResult<ParticipantDto>.Failure(ServiceErrorType.Conflict, "Esse e-mail ja foi convidado pra essa viagem.");

        var existingUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        var rawToken = TokenHasher.GenerateUrlSafeToken();

        var participant = new TripParticipant
        {
            TripId = tripId,
            UserId = existingUser?.Id,
            InvitedEmail = normalizedEmail,
            DisplayName = existingUser?.Name,
            Role = request.Role,
            Status = ParticipantStatus.Invited,
            InviteTokenHash = TokenHasher.Hash(rawToken),
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(InviteExpiryDays)
        };

        _db.TripParticipants.Add(participant);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _emailSender.SendAsync(normalizedEmail, "Convite pra uma viagem no TripFlow",
                $"<p>Voce foi convidado pra participar de uma viagem. Use o codigo <strong>{rawToken}</strong> " +
                $"no endpoint de aceitar convite (viagem {tripId}) pra confirmar. O convite expira em {InviteExpiryDays} dias.</p>", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de convite para {Email}", normalizedEmail);
        }

        await _tripNotifier.NotifyParticipantsChangedAsync(tripId, ct);
        return ServiceResult<ParticipantDto>.Success(ToDto(participant));
    }

    public async Task<ServiceResult<ParticipantDto>> AcceptInviteAsync(Guid tripId, Guid currentUserId, string currentUserEmail, string currentUserName, AcceptInviteRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = currentUserEmail.Trim().ToLowerInvariant();
        var participant = await _db.TripParticipants.FirstOrDefaultAsync(p => p.TripId == tripId && p.InvitedEmail == normalizedEmail, ct);

        if (participant is null || participant.InviteTokenHash is null || participant.InviteTokenExpiresAt is null)
            return ServiceResult<ParticipantDto>.Failure(ServiceErrorType.NotFound, "Convite nao encontrado.");

        if (participant.InviteTokenExpiresAt < DateTime.UtcNow || !TokenHasher.Matches(request.Token, participant.InviteTokenHash))
            return ServiceResult<ParticipantDto>.Failure(ServiceErrorType.Validation, "Convite invalido ou expirado.");

        participant.UserId = currentUserId;
        participant.DisplayName = currentUserName.Trim();
        participant.Status = ParticipantStatus.Accepted;
        participant.RespondedAt = DateTime.UtcNow;
        participant.InviteTokenHash = null;
        participant.InviteTokenExpiresAt = null;

        await _db.SaveChangesAsync(ct);

        var ownersToNotify = await _db.TripParticipants.AsNoTracking()
            .Where(p => p.TripId == tripId && p.Role == TripRole.Owner && p.Status == ParticipantStatus.Accepted
                && p.UserId != null && p.UserId != currentUserId)
            .Select(p => p.UserId!.Value)
            .ToListAsync(ct);

        foreach (var ownerId in ownersToNotify)
            await _notificationService.NotifyAsync(tripId, ownerId, NotificationType.ParticipantJoined, $"{currentUserName.Trim()} entrou na viagem.", ct);

        await _tripNotifier.NotifyParticipantsChangedAsync(tripId, ct);

        await _activityService.RecordAsync(tripId, participant.Id, currentUserId, ActivityType.ParticipantJoined, nameof(TripParticipant), participant.Id,
            $"{currentUserName.Trim()} entrou na viagem.", ct);

        return ServiceResult<ParticipantDto>.Success(ToDto(participant));
    }

    public async Task<ServiceResult<bool>> DeclineInviteAsync(Guid tripId, string currentUserEmail, CancellationToken ct = default)
    {
        var normalizedEmail = currentUserEmail.Trim().ToLowerInvariant();
        var participant = await _db.TripParticipants.FirstOrDefaultAsync(p => p.TripId == tripId && p.InvitedEmail == normalizedEmail, ct);

        if (participant is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Convite nao encontrado.");

        participant.Status = ParticipantStatus.Declined;
        participant.RespondedAt = DateTime.UtcNow;
        participant.InviteTokenHash = null;
        participant.InviteTokenExpiresAt = null;

        await _db.SaveChangesAsync(ct);
        await _tripNotifier.NotifyParticipantsChangedAsync(tripId, ct);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<ParticipantDto>> ListAsync(Guid tripId, CancellationToken ct = default)
    {
        var participants = await _db.TripParticipants.AsNoTracking().Where(p => p.TripId == tripId).ToListAsync(ct);
        return participants.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<bool>> UpdateRoleAsync(Guid tripId, Guid participantId, UpdateParticipantRoleRequest request, CancellationToken ct = default)
    {
        var participant = await _db.TripParticipants.FirstOrDefaultAsync(p => p.Id == participantId && p.TripId == tripId, ct);
        if (participant is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Participante nao encontrado.");

        if (participant.Role == TripRole.Owner && request.Role != TripRole.Owner && await IsLastOwnerAsync(tripId, participantId, ct))
            return ServiceResult<bool>.Failure(ServiceErrorType.Validation, "A viagem precisa ter pelo menos um dono.");

        participant.Role = request.Role;
        await _db.SaveChangesAsync(ct);
        await _tripNotifier.NotifyParticipantsChangedAsync(tripId, ct);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> RemoveAsync(Guid tripId, Guid participantId, CancellationToken ct = default)
    {
        var participant = await _db.TripParticipants.FirstOrDefaultAsync(p => p.Id == participantId && p.TripId == tripId, ct);
        if (participant is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Participante nao encontrado.");

        if (participant.Role == TripRole.Owner && await IsLastOwnerAsync(tripId, participantId, ct))
            return ServiceResult<bool>.Failure(ServiceErrorType.Validation, "A viagem precisa ter pelo menos um dono.");

        _db.TripParticipants.Remove(participant);
        await _db.SaveChangesAsync(ct);
        await _tripNotifier.NotifyParticipantsChangedAsync(tripId, ct);
        return ServiceResult<bool>.Success(true);
    }

    private async Task<bool> IsLastOwnerAsync(Guid tripId, Guid excludingParticipantId, CancellationToken ct)
    {
        var otherOwners = await _db.TripParticipants.CountAsync(p =>
            p.TripId == tripId && p.Id != excludingParticipantId && p.Role == TripRole.Owner && p.Status == ParticipantStatus.Accepted, ct);

        return otherOwners == 0;
    }

    private static ParticipantDto ToDto(TripParticipant p) => new(p.Id, p.TripId, p.UserId, p.InvitedEmail, p.DisplayName, p.Role, p.Status, p.CreatedAt);
}
