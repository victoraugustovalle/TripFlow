import { apiFetch } from "./client";
import type { ReservationDto, ReservationType } from "./types";

export interface ReservationInput {
  type: ReservationType;
  title: string;
  providerName: string | null;
  confirmationCode: string | null;
  startAt: string | null;
  endAt: string | null;
  location: string | null;
  latitude: number | null;
  longitude: number | null;
  price: number | null;
  currency: string | null;
  notes: string | null;
  itineraryItemId: string | null;
}

export function listReservations(tripId: string) {
  return apiFetch<ReservationDto[]>(`/api/trips/${tripId}/reservations`);
}

export function createReservation(tripId: string, input: ReservationInput) {
  return apiFetch<ReservationDto>(`/api/trips/${tripId}/reservations`, { method: "POST", body: input });
}

export function updateReservation(tripId: string, reservationId: string, input: ReservationInput) {
  return apiFetch<ReservationDto>(`/api/trips/${tripId}/reservations/${reservationId}`, { method: "PUT", body: input });
}

export function deleteReservation(tripId: string, reservationId: string) {
  return apiFetch<void>(`/api/trips/${tripId}/reservations/${reservationId}`, { method: "DELETE" });
}
