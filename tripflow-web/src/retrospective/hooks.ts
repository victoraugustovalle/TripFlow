import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getTripRetrospective, upsertTripMemory } from "../api/retrospective";
import type { UpsertTripMemoryInput } from "../api/retrospective";
import { pushToast } from "../toast/toastStore";

export function useTripRetrospective(tripId: string) {
  return useQuery({ queryKey: ["retrospective", tripId], queryFn: () => getTripRetrospective(tripId) });
}

export function useUpsertTripMemory(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpsertTripMemoryInput) => upsertTripMemory(tripId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["retrospective", tripId] });
      pushToast("Memoria salva.");
    },
  });
}
