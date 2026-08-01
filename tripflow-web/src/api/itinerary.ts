import { apiFetch } from "./client";
import type { ItineraryDayWeatherDto, ItineraryItemDto, ItineraryItemType } from "./types";

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

export interface ItineraryProposalOptionInput {
  title: string;
  description: string | null;
  location: string | null;
  latitude: number | null;
  longitude: number | null;
}

export interface ItineraryProposalInput {
  title: string;
  description: string | null;
  type: ItineraryItemType;
  itemDate: string;
  startTime: string | null;
  endTime: string | null;
  options: ItineraryProposalOptionInput[];
}

export function listItinerary(tripId: string) {
  return apiFetch<ItineraryItemDto[]>(`/api/trips/${tripId}/itinerary`);
}

export function getItineraryWeather(tripId: string) {
  return apiFetch<ItineraryDayWeatherDto[]>(`/api/trips/${tripId}/itinerary/weather`);
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

export function createItineraryProposal(tripId: string, input: ItineraryProposalInput) {
  return apiFetch<ItineraryItemDto>(`/api/trips/${tripId}/itinerary/proposals`, { method: "POST", body: input });
}

export function voteItineraryProposal(tripId: string, itemId: string, optionId: string) {
  return apiFetch<ItineraryItemDto>(`/api/trips/${tripId}/itinerary/${itemId}/vote`, { method: "POST", body: { optionId } });
}

export function confirmItineraryProposal(tripId: string, itemId: string, optionId: string) {
  return apiFetch<ItineraryItemDto>(`/api/trips/${tripId}/itinerary/${itemId}/confirm`, { method: "POST", body: { optionId } });
}
