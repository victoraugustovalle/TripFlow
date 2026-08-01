using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity;
using TripFlow.Application.Common;
using TripFlow.Application.Itinerary.DTOs;
using TripFlow.Application.Notifications;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Itinerary;

/// <summary>
/// Fluxo colaborativo de roteiro: em vez de um Editor cadastrar um item ja decidido, ele propoe
/// 2 ou mais opcoes concorrentes pro mesmo horario, os participantes votam, e o Editor confirma
/// a vencedora - que vira um ItineraryItem normal (Status=Confirmed), indistinguivel de um item
/// criado direto pelo ItineraryService. Cancelar uma proposta e so apagar o item (DeleteAsync do
/// ItineraryService ja funciona pra Status=Proposed - cascade limpa opcoes e votos).
/// </summary>
public class ItineraryProposalService
{
    private readonly IAppDbContext _db;
    private readonly ITripNotifier _tripNotifier;
    private readonly ActivityService _activityService;
    private readonly NotificationService _notificationService;

    public ItineraryProposalService(
        IAppDbContext db, ITripNotifier tripNotifier, ActivityService activityService, NotificationService notificationService)
    {
        _db = db;
        _tripNotifier = tripNotifier;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    public async Task<ItineraryItemDto> CreateAsync(Guid tripId, Guid actorUserId, CreateItineraryProposalRequest request, CancellationToken ct = default)
    {
        var item = new ItineraryItem
        {
            TripId = tripId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            ItemDate = request.ItemDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = ItineraryItemStatus.Proposed,
        };

        foreach (var option in request.Options)
        {
            item.ProposalOptions.Add(new ItineraryProposalOption
            {
                Title = option.Title.Trim(),
                Description = option.Description?.Trim(),
                Location = option.Location?.Trim(),
                Latitude = option.Latitude,
                Longitude = option.Longitude,
            });
        }

        _db.ItineraryItems.Add(item);
        await _db.SaveChangesAsync(ct);

        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ItineraryItemCreated, nameof(ItineraryItem), item.Id,
            $"{actorName} propos \"{item.Title}\" pro roteiro - vote na aba Roteiro.", ct);

        await _notificationService.NotifyTripAsync(tripId, actorUserId, NotificationType.ItineraryItemUpdated,
            $"{actorName} propos \"{item.Title}\" - vote na sua opcao favorita.", ct);

        var dto = ItineraryService.ToDto(item, actorParticipantId);
        await _tripNotifier.NotifyItineraryItemCreatedAsync(tripId, dto, ct);

        return dto;
    }

    public async Task<ServiceResult<ItineraryItemDto>> VoteAsync(
        Guid tripId, Guid actorUserId, Guid itemId, Guid optionId, CancellationToken ct = default)
    {
        var item = await _db.ItineraryItems
            .Include(i => i.ProposalOptions).ThenInclude(o => o.Votes)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.NotFound, "Item de roteiro nao encontrado.");

        if (item.Status != ItineraryItemStatus.Proposed)
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.Validation, "Essa proposta ja foi confirmada.");

        if (item.ProposalOptions.All(o => o.Id != optionId))
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.Validation, "Opcao nao pertence a essa proposta.");

        var (actorParticipantId, _) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        if (actorParticipantId is not { } participantId)
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.Unauthorized, "Voce precisa ser participante da viagem pra votar.");

        var existingVote = await _db.ItineraryVotes.FirstOrDefaultAsync(v => v.ItineraryItemId == itemId && v.ParticipantId == participantId, ct);
        if (existingVote is null)
            _db.ItineraryVotes.Add(new ItineraryVote { ItineraryItemId = itemId, OptionId = optionId, ParticipantId = participantId });
        else
            existingVote.OptionId = optionId;

        await _db.SaveChangesAsync(ct);

        // Reconsulta sem tracking em vez de confiar no fixup automatico do change tracker pra
        // contar os votos certo logo apos o upsert acima - mais simples e mais obviamente correto.
        var updatedItem = await _db.ItineraryItems.AsNoTracking()
            .Include(i => i.ProposalOptions).ThenInclude(o => o.Votes)
            .FirstAsync(i => i.Id == itemId, ct);

        var dto = ItineraryService.ToDto(updatedItem, participantId);
        await _tripNotifier.NotifyItineraryVoteChangedAsync(tripId, item.Id, ct);

        return ServiceResult<ItineraryItemDto>.Success(dto);
    }

    public async Task<ServiceResult<ItineraryItemDto>> ConfirmAsync(
        Guid tripId, Guid actorUserId, Guid itemId, Guid optionId, CancellationToken ct = default)
    {
        var item = await _db.ItineraryItems
            .Include(i => i.ProposalOptions)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.NotFound, "Item de roteiro nao encontrado.");

        if (item.Status != ItineraryItemStatus.Proposed)
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.Validation, "Essa proposta ja foi confirmada.");

        var winningOption = item.ProposalOptions.FirstOrDefault(o => o.Id == optionId);
        if (winningOption is null)
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.Validation, "Opcao nao pertence a essa proposta.");

        item.Title = winningOption.Title;
        item.Description = winningOption.Description ?? item.Description;
        item.Location = winningOption.Location;
        item.Latitude = winningOption.Latitude;
        item.Longitude = winningOption.Longitude;
        item.Status = ItineraryItemStatus.Confirmed;

        // A partir daqui e um ItineraryItem normal - as opcoes (vencedora inclusive, ja copiada
        // acima) e os votos (cascade a partir da opcao) deixam de fazer sentido.
        _db.ItineraryProposalOptions.RemoveRange(item.ProposalOptions.ToList());
        item.ProposalOptions.Clear();

        await _db.SaveChangesAsync(ct);

        // Mesmo padrao do UpdateAsync do ItineraryService: atualizacao de item nao gera entrada
        // na timeline de atividades (so criacao/remocao geram), so notificacao + tempo real.
        var (_, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);

        var dto = ItineraryService.ToDto(item);
        await _tripNotifier.NotifyItineraryItemUpdatedAsync(tripId, dto, ct);
        await _notificationService.NotifyTripAsync(tripId, actorUserId, NotificationType.ItineraryItemUpdated,
            $"{actorName} confirmou \"{item.Title}\" no roteiro.", ct);

        return ServiceResult<ItineraryItemDto>.Success(dto);
    }
}
