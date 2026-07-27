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
}
