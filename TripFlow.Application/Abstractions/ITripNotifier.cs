using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Expenses.DTOs;

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

    /// <summary>Avisa quem estiver com a viagem aberta que uma notificacao nova foi criada - sem
    /// payload, so um sinal pra recarregar a lista (o conteudo em si vem do GET /api/notifications).</summary>
    Task NotifyNotificationCreatedAsync(Guid tripId, CancellationToken cancellationToken = default);
}
