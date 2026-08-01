import { apiFetch } from "./client";
import type { ExpenseDto, PagedResult, SettlementDto, SettlementRecordDto } from "./types";

export interface ExpenseShareInput {
  participantId: string;
  shareAmount: number;
}

export interface CreateExpenseInput {
  description: string;
  amount: number;
  category: string;
  paidByParticipantId: string;
  expenseDate: string;
  splitBetweenParticipantIds: string[] | null;
  /** Quando informado, tem prioridade sobre splitBetweenParticipantIds - a soma precisa bater com amount. */
  shares: ExpenseShareInput[] | null;
}

export interface ExpenseFilters {
  category?: string;
  from?: string;
  to?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export function listExpenses(tripId: string, filters: ExpenseFilters = {}) {
  const params = new URLSearchParams();
  if (filters.category) params.set("category", filters.category);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.search) params.set("search", filters.search);
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 20));

  return apiFetch<PagedResult<ExpenseDto>>(`/api/trips/${tripId}/expenses?${params.toString()}`);
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

export interface MarkSettlementPaidInput {
  toParticipantId: string;
  amount: number;
}

export function markSettlementPaid(tripId: string, input: MarkSettlementPaidInput) {
  return apiFetch<SettlementRecordDto>(`/api/trips/${tripId}/expenses/settlement/mark-paid`, { method: "POST", body: input });
}

export function confirmSettlement(tripId: string, recordId: string) {
  return apiFetch<SettlementRecordDto>(`/api/trips/${tripId}/expenses/settlement/${recordId}/confirm`, { method: "POST" });
}
