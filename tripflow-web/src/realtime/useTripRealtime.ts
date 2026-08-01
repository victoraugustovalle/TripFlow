import * as signalR from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import type { ActivityLogEntryDto, PresenceUserDto } from "../api/types";
import { API_BASE_URL } from "../api/client";
import { useAuthStore, getAccessToken } from "../auth/authStore";
import { pushToast } from "../toast/toastStore";

/**
 * Conecta no hub da viagem e invalida as queries certas quando algo muda - deixa o
 * TanStack Query buscar os dados novos de novo, em vez de tentar remendar o cache na mao
 * (mais simples e menos sujeito a bug, ao custo de 1 fetch extra por evento). Tambem devolve
 * quem mais esta com essa viagem aberta agora (presenca via SignalR, nao persistido).
 */
export function useTripRealtime(tripId: string) {
  const queryClient = useQueryClient();
  const [onlineUsers, setOnlineUsers] = useState<PresenceUserDto[]>([]);

  useEffect(() => {
    setOnlineUsers([]);
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/trip`, { accessTokenFactory: () => getAccessToken() ?? "" })
      .withAutomaticReconnect()
      .build();

    const invalidateExpenses = () => {
      queryClient.invalidateQueries({ queryKey: ["expenses", tripId] });
      queryClient.invalidateQueries({ queryKey: ["settlement", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
    };
    const invalidateChecklist = () => {
      queryClient.invalidateQueries({ queryKey: ["checklist", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
    };
    const invalidateItinerary = () => {
      queryClient.invalidateQueries({ queryKey: ["itinerary", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
    };
    const invalidateReservations = () => queryClient.invalidateQueries({ queryKey: ["reservations", tripId] });
    const invalidateParticipants = () => {
      queryClient.invalidateQueries({ queryKey: ["participants", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
    };
    // Toast so pra acao de OUTRA pessoa - a sua propria acao ja tem feedback na hora (toast de
    // sucesso da mutation), duplicar viraria ruido.
    const onActivityCreated = (entry: ActivityLogEntryDto) => {
      queryClient.invalidateQueries({ queryKey: ["activity", tripId] });
      const myUserId = useAuthStore.getState().user?.id;
      if (entry.actorUserId && entry.actorUserId !== myUserId) {
        pushToast(entry.message, "info");
      }
    };
    const invalidateSettlement = () => {
      queryClient.invalidateQueries({ queryKey: ["settlement", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
    };

    connection.on("ExpenseCreated", invalidateExpenses);
    connection.on("ExpenseDeleted", invalidateExpenses);
    connection.on("ChecklistItemCreated", invalidateChecklist);
    connection.on("ChecklistItemUpdated", invalidateChecklist);
    connection.on("ChecklistItemDeleted", invalidateChecklist);
    connection.on("ItineraryItemCreated", invalidateItinerary);
    connection.on("ItineraryItemUpdated", invalidateItinerary);
    connection.on("ItineraryItemDeleted", invalidateItinerary);
    connection.on("ItineraryVoteChanged", invalidateItinerary);
    connection.on("ReservationCreated", invalidateReservations);
    connection.on("ReservationUpdated", invalidateReservations);
    connection.on("ReservationDeleted", invalidateReservations);
    connection.on("ParticipantsChanged", invalidateParticipants);
    connection.on("NotificationCreated", () => queryClient.invalidateQueries({ queryKey: ["notifications"] }));
    connection.on("ActivityCreated", onActivityCreated);
    connection.on("SettlementChanged", invalidateSettlement);
    connection.on("PresenceChanged", (users: PresenceUserDto[]) => setOnlineUsers(users));

    connection
      .start()
      .then(() => connection.invoke("JoinTrip", tripId))
      .catch((error) => console.error("Falha ao conectar no hub de tempo real", error));

    return () => {
      void connection.stop();
      setOnlineUsers([]);
    };
  }, [tripId, queryClient]);

  return { onlineUsers };
}
