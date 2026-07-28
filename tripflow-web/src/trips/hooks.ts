import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as tripsApi from "../api/trips";

export function useTrips() {
  return useQuery({ queryKey: ["trips"], queryFn: tripsApi.listMyTrips });
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

export function useDeleteTrip() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: tripsApi.deleteTrip,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["trips"] }),
  });
}
