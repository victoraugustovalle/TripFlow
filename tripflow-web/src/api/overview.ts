import { apiFetch } from "./client";
import type { TripOverviewDto } from "./types";

export function getTripOverview(tripId: string) {
  return apiFetch<TripOverviewDto>(`/api/trips/${tripId}/overview`);
}
