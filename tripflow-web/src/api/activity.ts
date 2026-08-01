import { apiFetch } from "./client";
import type { ActivityLogEntryDto, PagedResult } from "./types";

export function listTripActivity(tripId: string, page = 1, pageSize = 8) {
  return apiFetch<PagedResult<ActivityLogEntryDto>>(`/api/trips/${tripId}/activity?page=${page}&pageSize=${pageSize}`);
}
