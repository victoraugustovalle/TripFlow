import { apiFetch } from "./client";
import type { GeocodeResultDto } from "./types";

export function searchGeocode(query: string) {
  return apiFetch<GeocodeResultDto[]>(`/api/geocode?query=${encodeURIComponent(query)}`);
}
