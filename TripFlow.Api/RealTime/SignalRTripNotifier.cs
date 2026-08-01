using Microsoft.AspNetCore.SignalR;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity.DTOs;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Itinerary.DTOs;
using TripFlow.Application.Reservations.DTOs;

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

    public Task NotifyItineraryItemCreatedAsync(Guid tripId, ItineraryItemDto item, CancellationToken cancellationToken = default) =>
        Group(tripId).ItineraryItemCreated(item);

    public Task NotifyItineraryItemUpdatedAsync(Guid tripId, ItineraryItemDto item, CancellationToken cancellationToken = default) =>
        Group(tripId).ItineraryItemUpdated(item);

    public Task NotifyItineraryItemDeletedAsync(Guid tripId, Guid itemId, CancellationToken cancellationToken = default) =>
        Group(tripId).ItineraryItemDeleted(itemId);

    public Task NotifyReservationCreatedAsync(Guid tripId, ReservationDto reservation, CancellationToken cancellationToken = default) =>
        Group(tripId).ReservationCreated(reservation);

    public Task NotifyReservationUpdatedAsync(Guid tripId, ReservationDto reservation, CancellationToken cancellationToken = default) =>
        Group(tripId).ReservationUpdated(reservation);

    public Task NotifyReservationDeletedAsync(Guid tripId, Guid reservationId, CancellationToken cancellationToken = default) =>
        Group(tripId).ReservationDeleted(reservationId);

    public Task NotifyParticipantsChangedAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        Group(tripId).ParticipantsChanged();

    public Task NotifyNotificationCreatedAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        Group(tripId).NotificationCreated();

    public Task NotifyActivityCreatedAsync(Guid tripId, ActivityLogEntryDto entry, CancellationToken cancellationToken = default) =>
        Group(tripId).ActivityCreated(entry);

    public Task NotifySettlementChangedAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        Group(tripId).SettlementChanged();

    private ITripHubClient Group(Guid tripId) => _hubContext.Clients.Group(TripHub.GroupName(tripId));
}
