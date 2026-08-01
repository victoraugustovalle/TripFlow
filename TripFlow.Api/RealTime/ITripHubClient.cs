using TripFlow.Application.Activity.DTOs;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Expenses.DTOs;

namespace TripFlow.Api.RealTime;

/// <summary>Metodos que o servidor pode chamar no cliente conectado - tipado, pra nao errar nome/assinatura na mao (o mesmo padrao do LiveTranscribe).</summary>
public interface ITripHubClient
{
    Task ExpenseCreated(ExpenseDto expense);
    Task ExpenseDeleted(Guid expenseId);

    Task ChecklistItemCreated(ChecklistItemDto item);
    Task ChecklistItemUpdated(ChecklistItemDto item);
    Task ChecklistItemDeleted(Guid itemId);

    Task NotificationCreated();

    Task ActivityCreated(ActivityLogEntryDto entry);

    Task SettlementChanged();

    /// <summary>Lista completa de quem esta com essa viagem aberta agora (nao um delta) - mais
    /// simples pro cliente so substituir o estado local do que reconciliar entra/sai.</summary>
    Task PresenceChanged(IReadOnlyList<PresenceUser> onlineUsers);
}
