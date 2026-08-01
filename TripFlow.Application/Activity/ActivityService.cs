using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity.DTOs;
using TripFlow.Application.Common;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Activity;

/// <summary>
/// Grava e lista a timeline de eventos da viagem. Chamado pelos outros services (Expense,
/// Checklist, Itinerary, Reservation, Participant) nos mesmos pontos onde eles ja avisam o
/// ITripNotifier - RecordAsync tambem dispara o evento em tempo real, entao quem chama nao
/// precisa se preocupar com SignalR.
/// </summary>
public class ActivityService
{
    private readonly IAppDbContext _db;
    private readonly ITripNotifier _tripNotifier;

    public ActivityService(IAppDbContext db, ITripNotifier tripNotifier)
    {
        _db = db;
        _tripNotifier = tripNotifier;
    }

    /// <summary>Resolve o participante autor de uma acao a partir do usuario autenticado -
    /// usado pelos services pra montar a mensagem da atividade ("Fulano lancou um gasto...").
    /// Sem participante correspondente (ex: Admin global agindo numa viagem que nao e dele),
    /// cai no nome generico em vez de falhar a operacao principal.</summary>
    public async Task<(Guid? ParticipantId, string DisplayName)> ResolveActorAsync(Guid tripId, Guid userId, CancellationToken ct = default)
    {
        var participant = await _db.TripParticipants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TripId == tripId && p.UserId == userId, ct);

        return (participant?.Id, participant?.DisplayName ?? participant?.InvitedEmail ?? "Alguem");
    }

    public async Task RecordAsync(
        Guid tripId, Guid? actorParticipantId, Guid? actorUserId, ActivityType type, string entityType, Guid? entityId, string message,
        CancellationToken ct = default)
    {
        var entry = new ActivityLogEntry
        {
            TripId = tripId,
            ActorParticipantId = actorParticipantId,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            Message = message
        };

        _db.ActivityLogEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        await _tripNotifier.NotifyActivityCreatedAsync(tripId, ToDto(entry, null, actorUserId), ct);
    }

    public async Task<PagedResult<ActivityLogEntryDto>> ListAsync(Guid tripId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.ActivityLogEntries.AsNoTracking()
            .Include(a => a.ActorParticipant)
            .Where(a => a.TripId == tripId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var entries = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ActivityLogEntryDto>(
            entries.Select(e => ToDto(e, e.ActorParticipant, e.ActorParticipant?.UserId)).ToList(), totalCount, page, pageSize);
    }

    private static ActivityLogEntryDto ToDto(ActivityLogEntry entry, TripParticipant? actor, Guid? actorUserId) => new(
        entry.Id, entry.Type, entry.EntityType, entry.EntityId, entry.Message,
        actor?.DisplayName ?? actor?.InvitedEmail, actorUserId, entry.CreatedAt);
}
