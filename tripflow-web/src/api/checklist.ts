import { apiFetch } from "./client";
import type { ChecklistItemDto } from "./types";

export function listChecklist(tripId: string) {
  return apiFetch<ChecklistItemDto[]>(`/api/trips/${tripId}/checklist`);
}

export function createChecklistItem(tripId: string, title: string, assignedToParticipantId?: string | null, dueDate?: string | null) {
  return apiFetch<ChecklistItemDto>(`/api/trips/${tripId}/checklist`, {
    method: "POST",
    body: { title, assignedToParticipantId: assignedToParticipantId ?? null, dueDate: dueDate ?? null },
  });
}

export function updateChecklistItem(
  tripId: string,
  itemId: string,
  input: { title: string; isDone: boolean; assignedToParticipantId?: string | null; dueDate?: string | null },
) {
  return apiFetch<ChecklistItemDto>(`/api/trips/${tripId}/checklist/${itemId}`, {
    method: "PUT",
    body: { ...input, assignedToParticipantId: input.assignedToParticipantId ?? null, dueDate: input.dueDate ?? null },
  });
}

export function deleteChecklistItem(tripId: string, itemId: string) {
  return apiFetch<void>(`/api/trips/${tripId}/checklist/${itemId}`, { method: "DELETE" });
}
