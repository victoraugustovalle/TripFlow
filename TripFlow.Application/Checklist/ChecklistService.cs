using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Common;
using TripFlow.Application.Notifications;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Checklist;

public class ChecklistService
{
    private readonly IAppDbContext _db;
    private readonly ITripNotifier _tripNotifier;
    private readonly NotificationService _notificationService;
    private readonly ActivityService _activityService;

    public ChecklistService(IAppDbContext db, ITripNotifier tripNotifier, NotificationService notificationService, ActivityService activityService)
    {
        _db = db;
        _tripNotifier = tripNotifier;
        _notificationService = notificationService;
        _activityService = activityService;
    }

    public async Task<ChecklistItemDto> CreateAsync(Guid tripId, Guid actorUserId, CreateChecklistItemRequest request, CancellationToken ct = default)
    {
        var item = new ChecklistItem
        {
            TripId = tripId,
            Title = request.Title.Trim(),
            AssignedToParticipantId = request.AssignedToParticipantId,
            DueDate = request.DueDate
        };

        _db.ChecklistItems.Add(item);
        await _db.SaveChangesAsync(ct);

        var dto = ToDto(item);
        await _tripNotifier.NotifyChecklistItemCreatedAsync(tripId, dto, ct);

        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ChecklistItemCreated, nameof(ChecklistItem), item.Id,
            $"{actorName} adicionou \"{item.Title}\" ao checklist.", ct);

        if (request.AssignedToParticipantId is { } assignedId)
            await NotifyAssignmentAsync(tripId, assignedId, item.Title, actorParticipantId, actorUserId, actorName, ct);

        return dto;
    }

    public async Task<IReadOnlyList<ChecklistItemDto>> ListAsync(Guid tripId, CancellationToken ct = default)
    {
        var items = await _db.ChecklistItems.AsNoTracking()
            .Where(c => c.TripId == tripId)
            .OrderBy(c => c.IsDone).ThenBy(c => c.DueDate)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<ChecklistItemDto>> UpdateAsync(Guid tripId, Guid actorUserId, Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct = default)
    {
        var item = await _db.ChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId && c.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<ChecklistItemDto>.Failure(ServiceErrorType.NotFound, "Item nao encontrado.");

        var previousAssignee = item.AssignedToParticipantId;

        item.Title = request.Title.Trim();
        item.IsDone = request.IsDone;
        item.AssignedToParticipantId = request.AssignedToParticipantId;
        item.DueDate = request.DueDate;

        await _db.SaveChangesAsync(ct);

        var dto = ToDto(item);
        await _tripNotifier.NotifyChecklistItemUpdatedAsync(tripId, dto, ct);

        // So notifica quando a atribuicao MUDA pra alguem novo - senao toda edicao (marcar
        // feito, mudar prazo) do mesmo item ja atribuido geraria notificacao repetida.
        if (request.AssignedToParticipantId is { } assignedId && assignedId != previousAssignee)
        {
            var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
            await NotifyAssignmentAsync(tripId, assignedId, item.Title, actorParticipantId, actorUserId, actorName, ct);
        }

        return ServiceResult<ChecklistItemDto>.Success(dto);
    }

    private async Task NotifyAssignmentAsync(
        Guid tripId, Guid assigneeParticipantId, string title, Guid? actorParticipantId, Guid actorUserId, string actorName, CancellationToken ct)
    {
        var assignee = await _db.TripParticipants.AsNoTracking()
            .Where(p => p.Id == assigneeParticipantId)
            .Select(p => new { p.UserId, p.DisplayName, p.InvitedEmail })
            .FirstOrDefaultAsync(ct);

        if (assignee is null)
            return;

        if (assignee.UserId is { } userId)
            await _notificationService.NotifyAsync(tripId, userId, NotificationType.ChecklistItemAssigned, $"Voce ficou responsavel por: \"{title}\".", ct);

        var assigneeName = assignee.DisplayName ?? assignee.InvitedEmail;
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ChecklistItemAssigned, nameof(ChecklistItem), null,
            $"{actorName} atribuiu \"{title}\" a {assigneeName}.", ct);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid actorUserId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.ChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId && c.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Item nao encontrado.");

        var title = item.Title;

        _db.ChecklistItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        await _tripNotifier.NotifyChecklistItemDeletedAsync(tripId, itemId, ct);

        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ChecklistItemDeleted, nameof(ChecklistItem), itemId,
            $"{actorName} removeu \"{title}\" do checklist.", ct);

        return ServiceResult<bool>.Success(true);
    }

    private static ChecklistItemDto ToDto(ChecklistItem item) => new(item.Id, item.Title, item.IsDone, item.AssignedToParticipantId, item.DueDate, item.CreatedAt);
}
