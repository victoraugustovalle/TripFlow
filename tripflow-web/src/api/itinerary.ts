import { apiFetch } from "./client";
import type { ItineraryItemDto, ItineraryItemType } from "./types";

export interface ItineraryItemInput {
  title: string;
  description: string | null;
  type: ItineraryItemType;
  itemDate: string;
  startTime: string | null;
  endTime: string | null;
  location: string | null;
  latitude: number | null;
  longitude: number | null;
}

export function listItinerary(tripId: string) {
  return apiFetch<ItineraryItemDto[]>(`/api/trips/${tripId}/itinerary`);
}

export function createItineraryItem(tripId: string, input: ItineraryItemInput) {
  return apiFetch<ItineraryItemDto>(`/api/trips/${tripId}/itinerary`, { method: "POST", body: input });
}

export function updateItineraryItem(tripId: string, itemId: string, input: ItineraryItemInput) {
  return apiFetch<ItineraryItemDto>(`/api/trips/${tripId}/itinerary/${itemId}`, { method: "PUT", body: input });
}

export function deleteItineraryItem(tripId: string, itemId: string) {
  return apiFetch<void>(`/api/trips/${tripId}/itinerary/${itemId}`, { method: "DELETE" });
}
