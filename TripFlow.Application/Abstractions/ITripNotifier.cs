using TripFlow.Application.Activity.DTOs;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Itinerary.DTOs;
using TripFlow.Application.Reservations.DTOs;

namespace TripFlow.Application.Abstractions;

/// <summary>
/// Avisa quem estiver com a viagem aberta (via SignalR) quando algo muda, sem precisar
/// dar F5. A Application so conhece essa interface - quem implementa de verdade (Hub do
/// SignalR) fica na Api, que e onde faz sentido depender de ASP.NET Core SignalR.
/// </summary>
public interface ITripNotifier
{
    Task NotifyExpenseCreatedAsync(Guid tripId, ExpenseDto expense, CancellationToken cancellationToken = default);
    Task NotifyExpenseDeletedAsync(Guid tripId, Guid expenseId, CancellationToken cancellationToken = default);

    Task NotifyChecklistItemCreatedAsync(Guid tripId, ChecklistItemDto item, CancellationToken cancellationToken = default);
    Task NotifyChecklistItemUpdatedAsync(Guid tripId, ChecklistItemDto item, CancellationToken cancellationToken = default);
    Task NotifyChecklistItemDeletedAsync(Guid tripId, Guid itemId, CancellationToken cancellationToken = default);

    Task NotifyItineraryItemCreatedAsync(Guid tripId, ItineraryItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItineraryItemUpdatedAsync(Guid tripId, ItineraryItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItineraryItemDeletedAsync(Guid tripId, Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Sinal generico (sem payload de votos) pra "a contagem de votos dessa proposta
    /// mudou" - o cliente so refaz o GET da lista de roteiro, igual ja faz pra ItineraryItemUpdated.</summary>
    Task NotifyItineraryVoteChangedAsync(Guid tripId, Guid itemId, CancellationToken cancellationToken = default);

    Task NotifyReservationCreatedAsync(Guid tripId, ReservationDto reservation, CancellationToken cancellationToken = default);
    Task NotifyReservationUpdatedAsync(Guid tripId, ReservationDto reservation, CancellationToken cancellationToken = default);
    Task NotifyReservationDeletedAsync(Guid tripId, Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>Sinal generico pra qualquer mudanca na lista de participantes (convite, aceite,
    /// recusa, troca de papel, remocao) - sem payload, so um sinal pra recarregar a lista.</summary>
    Task NotifyParticipantsChangedAsync(Guid tripId, CancellationToken cancellationToken = default);

    /// <summary>Avisa quem estiver com a viagem aberta que uma notificacao nova foi criada - sem
    /// payload, so um sinal pra recarregar a lista (o conteudo em si vem do GET /api/notifications).</summary>
    Task NotifyNotificationCreatedAsync(Guid tripId, CancellationToken cancellationToken = default);

    /// <summary>Avisa quem estiver com a viagem aberta que um evento novo entrou na timeline -
    /// com payload, pra inserir direto na lista sem precisar de um refetch.</summary>
    Task NotifyActivityCreatedAsync(Guid tripId, ActivityLogEntryDto entry, CancellationToken cancellationToken = default);

    /// <summary>Sinal generico pra qualquer mudanca no settlement (quitacao marcada como paga
    /// ou confirmada) - sem payload, o cliente recalcula buscando GET .../expenses/settlement de novo.</summary>
    Task NotifySettlementChangedAsync(Guid tripId, CancellationToken cancellationToken = default);
}
