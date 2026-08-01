import { apiFetch } from "./client";
import type { BudgetDto } from "./types";

export function listBudgets(tripId: string) {
  return apiFetch<BudgetDto[]>(`/api/trips/${tripId}/budgets`);
}

export function upsertBudget(tripId: string, category: string, plannedAmount: number) {
  return apiFetch<BudgetDto>(`/api/trips/${tripId}/budgets`, {
    method: "PUT",
    body: { category, plannedAmount },
  });
}

export function deleteBudget(tripId: string, budgetId: string) {
  return apiFetch<void>(`/api/trips/${tripId}/budgets/${budgetId}`, { method: "DELETE" });
}
