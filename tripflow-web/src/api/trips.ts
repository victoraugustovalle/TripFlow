import { apiFetch } from "./client";
import type { TripDto, TripSummaryDto } from "./types";

export interface CreateTripInput {
  name: string;
  destination?: string | null;
  description?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  currency: string;
}

export interface UpdateTripInput {
  name: string;
  destination: string | null;
  description: string | null;
  startDate: string | null;
  endDate: string | null;
  status: number;
  coverImageUrl: string | null;
}

export function listMyTrips() {
  return apiFetch<TripSummaryDto[]>("/api/trips");
}

export function getTrip(tripId: string) {
  return apiFetch<TripDto>(`/api/trips/${tripId}`);
}

export function createTrip(input: CreateTripInput) {
  return apiFetch<TripDto>("/api/trips", { method: "POST", body: input });
}

export function updateTrip(tripId: string, input: UpdateTripInput) {
  return apiFetch<TripDto>(`/api/trips/${tripId}`, { method: "PUT", body: input });
}

export function deleteTrip(tripId: string) {
  return apiFetch<void>(`/api/trips/${tripId}`, { method: "DELETE" });
}
