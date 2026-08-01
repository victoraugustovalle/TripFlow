using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity;
using TripFlow.Application.Common;
using TripFlow.Application.Itinerary.DTOs;
using TripFlow.Application.Notifications;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Itinerary;

public class ItineraryService
{
    private readonly IAppDbContext _db;
    private readonly ITripNotifier _tripNotifier;
    private readonly ActivityService _activityService;
    private readonly NotificationService _notificationService;

    public ItineraryService(IAppDbContext db, ITripNotifier tripNotifier, ActivityService activityService, NotificationService notificationService)
    {
        _db = db;
        _tripNotifier = tripNotifier;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    public async Task<ItineraryItemDto> CreateAsync(Guid tripId, Guid actorUserId, CreateItineraryItemRequest request, CancellationToken ct = default)
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
            Location = request.Location?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.ItineraryItems.Add(item);
        await _db.SaveChangesAsync(ct);

        var dto = ToDto(item);
        await _tripNotifier.NotifyItineraryItemCreatedAsync(tripId, dto, ct);

        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ItineraryItemCreated, nameof(ItineraryItem), item.Id,
            $"{actorName} adicionou \"{item.Title}\" ao roteiro.", ct);

        return dto;
    }

    public async Task<IReadOnlyList<ItineraryItemDto>> ListAsync(Guid tripId, Guid actorUserId, CancellationToken ct = default)
    {
        var items = await _db.ItineraryItems.AsNoTracking()
            .Include(i => i.ProposalOptions).ThenInclude(o => o.Votes)
            .Where(i => i.TripId == tripId)
            .OrderBy(i => i.ItemDate).ThenBy(i => i.StartTime)
            .ToListAsync(ct);

        var myParticipantId = await _db.TripParticipants.AsNoTracking()
            .Where(p => p.TripId == tripId && p.UserId == actorUserId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        return items.Select(item => ToDto(item, myParticipantId)).ToList();
    }

    public async Task<ServiceResult<ItineraryItemDto>> UpdateAsync(Guid tripId, Guid actorUserId, Guid itemId, UpdateItineraryItemRequest request, CancellationToken ct = default)
    {
        var item = await _db.ItineraryItems.FirstOrDefaultAsync(i => i.Id == itemId && i.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.NotFound, "Item de roteiro nao encontrado.");

        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim();
        item.Type = request.Type;
        item.ItemDate = request.ItemDate;
        item.StartTime = request.StartTime;
        item.EndTime = request.EndTime;
        item.Location = request.Location?.Trim();
        item.Latitude = request.Latitude;
        item.Longitude = request.Longitude;

        await _db.SaveChangesAsync(ct);

        var dto = ToDto(item);
        await _tripNotifier.NotifyItineraryItemUpdatedAsync(tripId, dto, ct);

        var (_, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _notificationService.NotifyTripAsync(tripId, actorUserId, NotificationType.ItineraryItemUpdated, $"{actorName} atualizou \"{item.Title}\" no roteiro.", ct);

        return ServiceResult<ItineraryItemDto>.Success(dto);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid actorUserId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.ItineraryItems.FirstOrDefaultAsync(i => i.Id == itemId && i.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Item de roteiro nao encontrado.");

        var title = item.Title;

        _db.ItineraryItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        await _tripNotifier.NotifyItineraryItemDeletedAsync(tripId, itemId, ct);

        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ItineraryItemDeleted, nameof(ItineraryItem), itemId,
            $"{actorName} removeu \"{title}\" do roteiro.", ct);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>internal (nao private) pra ItineraryProposalService reaproveitar o mesmo
    /// mapeamento em vez de duplicar a montagem de ProposalOptions/MyVotedOptionId.</summary>
    internal static ItineraryItemDto ToDto(ItineraryItem item, Guid? myParticipantId = null)
    {
        var options = item.ProposalOptions
            .Select(o => new ItineraryProposalOptionDto(o.Id, o.Title, o.Description, o.Location, o.Latitude, o.Longitude, o.Votes.Count))
            .ToList();

        var myVotedOptionId = myParticipantId is { } participantId
            ? item.ProposalOptions.SelectMany(o => o.Votes).FirstOrDefault(v => v.ParticipantId == participantId)?.OptionId
            : null;

        return new ItineraryItemDto(
            item.Id, item.Title, item.Description, item.Type, item.ItemDate, item.StartTime, item.EndTime,
            item.Location, item.Latitude, item.Longitude, item.CreatedAt, item.Status, options, myVotedOptionId);
    }
}
