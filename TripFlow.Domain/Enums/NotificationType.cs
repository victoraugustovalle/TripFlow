namespace TripFlow.Domain.Enums;

public enum NotificationType
{
    ParticipantJoined = 0,
    ChecklistItemAssigned = 1,
    ExpenseCreated = 2,
    BudgetExceeded = 3,
    ReservationCreated = 4,
    ReservationUpdated = 5,
    ItineraryItemUpdated = 6,
    DocumentDeleted = 7,
    SettlementMarkedPaid = 8,
    SettlementConfirmed = 9,

    /// <summary>Proativo (disparado pelo job diario, nao por uma acao de usuario): a viagem
    /// esta chegando e a prontidao (TripReadinessService) ainda nao esta em 100%.</summary>
    TripStartingSoonNotReady = 10,

    /// <summary>Proativo: no ritmo atual de gasto, uma categoria de orcamento deve estourar
    /// antes do fim da viagem (calculado por dias decorridos vs. valor gasto).</summary>
    BudgetPaceExceeding = 11,
}
