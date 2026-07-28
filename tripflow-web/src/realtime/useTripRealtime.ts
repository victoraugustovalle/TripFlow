import * as signalR from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { API_BASE_URL } from "../api/client";
import { getAccessToken } from "../auth/authStore";

/**
 * Conecta no hub da viagem e invalida as queries certas quando algo muda - deixa o
 * TanStack Query buscar os dados novos de novo, em vez de tentar remendar o cache na mao
 * (mais simples e menos sujeito a bug, ao custo de 1 fetch extra por evento).
 */
export function useTripRealtime(tripId: string) {
  const queryClient = useQueryClient();

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/trip`, { accessTokenFactory: () => getAccessToken() ?? "" })
      .withAutomaticReconnect()
      .build();

    const invalidateExpenses = () => {
      queryClient.invalidateQueries({ queryKey: ["expenses", tripId] });
      queryClient.invalidateQueries({ queryKey: ["settlement", tripId] });
    };
    const invalidateChecklist = () => queryClient.invalidateQueries({ queryKey: ["checklist", tripId] });

    connection.on("ExpenseCreated", invalidateExpenses);
    connection.on("ExpenseDeleted", invalidateExpenses);
    connection.on("ChecklistItemCreated", invalidateChecklist);
    connection.on("ChecklistItemUpdated", invalidateChecklist);
    connection.on("ChecklistItemDeleted", invalidateChecklist);

    connection
      .start()
      .then(() => connection.invoke("JoinTrip", tripId))
      .catch((error) => console.error("Falha ao conectar no hub de tempo real", error));

    return () => {
      void connection.stop();
    };
  }, [tripId, queryClient]);
}
