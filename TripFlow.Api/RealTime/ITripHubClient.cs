using TripFlow.Application.Activity.DTOs;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Itinerary.DTOs;
using TripFlow.Application.Reservations.DTOs;

namespace TripFlow.Api.RealTime;

/// <summary>Metodos que o servidor pode chamar no cliente conectado - tipado, pra nao errar nome/assinatura na mao (o mesmo padrao do LiveTranscribe).</summary>
public interface ITripHubClient
{
    Task ExpenseCreated(ExpenseDto expense);
    Task ExpenseDeleted(Guid expenseId);

    Task ChecklistItemCreated(ChecklistItemDto item);
    Task ChecklistItemUpdated(ChecklistItemDto item);
    Task ChecklistItemDeleted(Guid itemId);

    Task ItineraryItemCreated(ItineraryItemDto item);
    Task ItineraryItemUpdated(ItineraryItemDto item);
    Task ItineraryItemDeleted(Guid itemId);

    Task ReservationCreated(ReservationDto reservation);
    Task ReservationUpdated(ReservationDto reservation);
    Task ReservationDeleted(Guid reservationId);

    /// <summary>Sinal generico pra convite/papel/remocao de participante - o cliente so precisa
    /// recarregar a lista, nao importa qual dos eventos foi (convidar, aceitar, recusar, mudar
    /// papel, remover).</summary>
    Task ParticipantsChanged();

    Task NotificationCreated();

    Task ActivityCreated(ActivityLogEntryDto entry);

    Task SettlementChanged();

    /// <summary>Lista completa de quem esta com essa viagem aberta agora (nao um delta) - mais
    /// simples pro cliente so substituir o estado local do que reconciliar entra/sai.</summary>
    Task PresenceChanged(IReadOnlyList<PresenceUser> onlineUsers);
}
