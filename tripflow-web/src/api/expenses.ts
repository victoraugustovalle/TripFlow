import { apiFetch } from "./client";
import type { ExpenseDto, SettlementDto } from "./types";

export interface CreateExpenseInput {
  description: string;
  amount: number;
  category: string;
  paidByParticipantId: string;
  expenseDate: string;
  splitBetweenParticipantIds: string[] | null;
}

export function listExpenses(tripId: string) {
  return apiFetch<ExpenseDto[]>(`/api/trips/${tripId}/expenses`);
}

export function createExpense(tripId: string, input: CreateExpenseInput) {
  return apiFetch<ExpenseDto>(`/api/trips/${tripId}/expenses`, { method: "POST", body: input });
}

export function deleteExpense(tripId: string, expenseId: string) {
  return apiFetch<void>(`/api/trips/${tripId}/expenses/${expenseId}`, { method: "DELETE" });
}

export function getSettlement(tripId: string) {
  return apiFetch<SettlementDto>(`/api/trips/${tripId}/expenses/settlement`);
}
