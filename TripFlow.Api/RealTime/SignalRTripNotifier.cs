using Microsoft.AspNetCore.SignalR;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity.DTOs;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Expenses.DTOs;

namespace TripFlow.Api.RealTime;

public class SignalRTripNotifier : ITripNotifier
{
    private readonly IHubContext<TripHub, ITripHubClient> _hubContext;

    public SignalRTripNotifier(IHubContext<TripHub, ITripHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyExpenseCreatedAsync(Guid tripId, ExpenseDto expense, CancellationToken cancellationToken = default) =>
        Group(tripId).ExpenseCreated(expense);

    public Task NotifyExpenseDeletedAsync(Guid tripId, Guid expenseId, CancellationToken cancellationToken = default) =>
        Group(tripId).ExpenseDeleted(expenseId);

    public Task NotifyChecklistItemCreatedAsync(Guid tripId, ChecklistItemDto item, CancellationToken cancellationToken = default) =>
        Group(tripId).ChecklistItemCreated(item);

    public Task NotifyChecklistItemUpdatedAsync(Guid tripId, ChecklistItemDto item, CancellationToken cancellationToken = default) =>
        Group(tripId).ChecklistItemUpdated(item);

    public Task NotifyChecklistItemDeletedAsync(Guid tripId, Guid itemId, CancellationToken cancellationToken = default) =>
        Group(tripId).ChecklistItemDeleted(itemId);

    public Task NotifyNotificationCreatedAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        Group(tripId).NotificationCreated();

    public Task NotifyActivityCreatedAsync(Guid tripId, ActivityLogEntryDto entry, CancellationToken cancellationToken = default) =>
        Group(tripId).ActivityCreated(entry);

    public Task NotifySettlementChangedAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        Group(tripId).SettlementChanged();

    private ITripHubClient Group(Guid tripId) => _hubContext.Clients.Group(TripHub.GroupName(tripId));
}
