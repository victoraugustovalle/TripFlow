import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as reservationsApi from "../api/reservations";
import type { ReservationInput } from "../api/reservations";
import { pushToast } from "../toast/toastStore";

export function useReservations(tripId: string) {
  return useQuery({ queryKey: ["reservations", tripId], queryFn: () => reservationsApi.listReservations(tripId) });
}

export function useCreateReservation(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ReservationInput) => reservationsApi.createReservation(tripId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reservations", tripId] });
      pushToast("Reserva adicionada.");
    },
  });
}

export function useUpdateReservation(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ reservationId, input }: { reservationId: string; input: ReservationInput }) =>
      reservationsApi.updateReservation(tripId, reservationId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reservations", tripId] });
      pushToast("Reserva atualizada.");
    },
  });
}

export function useDeleteReservation(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (reservationId: string) => reservationsApi.deleteReservation(tripId, reservationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reservations", tripId] });
      pushToast("Reserva removida.");
    },
  });
}
