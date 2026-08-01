import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as journalApi from "../api/journal";
import * as tripsApi from "../api/trips";
import type { UpdateTripInput } from "../api/trips";

export function useTrips() {
  return useQuery({ queryKey: ["trips"], queryFn: tripsApi.listMyTrips });
}

/** Cross-trip: a linha do tempo pessoal de viagens concluidas, ao contrario de useTrips (que
 * lista as viagens em qualquer status). Compartilha o ciclo de vida da lista de viagens - uma
 * viagem so entra aqui quando o status muda pra Completed, o que invalida ["trips"]. */
export function useMyJournal() {
  return useQuery({ queryKey: ["trips", "journal"], queryFn: journalApi.getMyJournal });
}

export function useTrip(tripId: string) {
  return useQuery({ queryKey: ["trips", tripId], queryFn: () => tripsApi.getTrip(tripId) });
}

export function useCreateTrip() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: tripsApi.createTrip,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["trips"] }),
  });
}

export function useUpdateTrip(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateTripInput) => tripsApi.updateTrip(tripId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trips", tripId] });
      queryClient.invalidateQueries({ queryKey: ["trips"] });
    },
  });
}

export function useDeleteTrip() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: tripsApi.deleteTrip,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["trips"] }),
  });
}
