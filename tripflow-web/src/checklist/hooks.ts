import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as checklistApi from "../api/checklist";
import type { ChecklistItemDto } from "../api/types";
import { pushToast } from "../toast/toastStore";

export function useChecklist(tripId: string) {
  return useQuery({ queryKey: ["checklist", tripId], queryFn: () => checklistApi.listChecklist(tripId) });
}

export interface CreateChecklistItemInput {
  title: string;
  assignedToParticipantId: string | null;
  dueDate: string | null;
}

export function useCreateChecklistItem(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateChecklistItemInput) =>
      checklistApi.createChecklistItem(tripId, input.title, input.assignedToParticipantId, input.dueDate),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["checklist", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Item adicionado a checklist.");
    },
  });
}

export function useToggleChecklistItem(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (item: ChecklistItemDto) =>
      checklistApi.updateChecklistItem(tripId, item.id, {
        title: item.title,
        isDone: !item.isDone,
        assignedToParticipantId: item.assignedToParticipantId,
        dueDate: item.dueDate,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["checklist", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
    },
  });
}

export function useDeleteChecklistItem(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (itemId: string) => checklistApi.deleteChecklistItem(tripId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["checklist", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Item removido da checklist.");
    },
  });
}
