namespace TripFlow.Domain.Enums;

public enum ActivityType
{
    ExpenseCreated = 0,
    ExpenseDeleted = 1,
    ChecklistItemCreated = 2,
    ChecklistItemAssigned = 3,
    ChecklistItemDeleted = 4,
    ItineraryItemCreated = 5,
    ItineraryItemDeleted = 6,
    ReservationCreated = 7,
    ReservationDeleted = 8,
    ParticipantJoined = 9,
    SettlementMarkedPaid = 10,
    SettlementConfirmed = 11,
}
