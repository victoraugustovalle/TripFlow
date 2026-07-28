import { apiFetch } from "./client";
import type { ParticipantDto, TripRole } from "./types";

export function listParticipants(tripId: string) {
  return apiFetch<ParticipantDto[]>(`/api/trips/${tripId}/participants`);
}

export function inviteParticipant(tripId: string, email: string, role: TripRole) {
  return apiFetch<ParticipantDto>(`/api/trips/${tripId}/participants/invite`, { method: "POST", body: { email, role } });
}

export function acceptInvite(tripId: string, token: string) {
  return apiFetch<ParticipantDto>(`/api/trips/${tripId}/participants/accept`, { method: "POST", body: { token } });
}

export function declineInvite(tripId: string) {
  return apiFetch<void>(`/api/trips/${tripId}/participants/decline`, { method: "POST" });
}

export function updateParticipantRole(tripId: string, participantId: string, role: TripRole) {
  return apiFetch<void>(`/api/trips/${tripId}/participants/${participantId}/role`, { method: "PUT", body: { role } });
}

export function removeParticipant(tripId: string, participantId: string) {
  return apiFetch<void>(`/api/trips/${tripId}/participants/${participantId}`, { method: "DELETE" });
}
