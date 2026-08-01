import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as participantsApi from "../api/participants";
import type { TripRole } from "../api/types";
import { pushToast } from "../toast/toastStore";

export function useParticipants(tripId: string) {
  return useQuery({ queryKey: ["participants", tripId], queryFn: () => participantsApi.listParticipants(tripId) });
}

export function useInviteParticipant(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ email, role }: { email: string; role: TripRole }) => participantsApi.inviteParticipant(tripId, email, role),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["participants", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Convite enviado.");
    },
  });
}

export function useRemoveParticipant(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (participantId: string) => participantsApi.removeParticipant(tripId, participantId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["participants", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Participante removido.");
    },
  });
}
