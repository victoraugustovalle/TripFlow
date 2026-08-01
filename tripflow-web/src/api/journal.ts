import { apiFetch } from "./client";
import type { MyJournalDto } from "./types";

export function getMyJournal() {
  return apiFetch<MyJournalDto>("/api/me/journal");
}
