import { apiFetch } from "./client";
import type { TripMemoryDto, TripRetrospectiveDto } from "./types";

export function getTripRetrospective(tripId: string) {
  return apiFetch<TripRetrospectiveDto>(`/api/trips/${tripId}/retrospective`);
}

export interface UpsertTripMemoryInput {
  highlight: string | null;
  rating: number | null;
  photoUrl: string | null;
}

export function upsertTripMemory(tripId: string, input: UpsertTripMemoryInput) {
  return apiFetch<TripMemoryDto>(`/api/trips/${tripId}/retrospective/memory`, { method: "PUT", body: input });
}
