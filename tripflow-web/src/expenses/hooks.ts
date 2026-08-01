import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as expensesApi from "../api/expenses";
import type { CreateExpenseInput, ExpenseFilters } from "../api/expenses";
import { pushToast } from "../toast/toastStore";

export function useExpenses(tripId: string, filters: ExpenseFilters = {}) {
  return useQuery({
    queryKey: ["expenses", tripId, filters],
    queryFn: () => expensesApi.listExpenses(tripId, filters),
    placeholderData: keepPreviousData,
  });
}

export function useSettlement(tripId: string) {
  return useQuery({ queryKey: ["settlement", tripId], queryFn: () => expensesApi.getSettlement(tripId) });
}

export function useCreateExpense(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateExpenseInput) => expensesApi.createExpense(tripId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["expenses", tripId] });
      queryClient.invalidateQueries({ queryKey: ["settlement", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Gasto adicionado.");
    },
  });
}

export function useDeleteExpense(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (expenseId: string) => expensesApi.deleteExpense(tripId, expenseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["expenses", tripId] });
      queryClient.invalidateQueries({ queryKey: ["settlement", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Gasto removido.");
    },
  });
}

export function useMarkSettlementPaid(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: expensesApi.MarkSettlementPaidInput) => expensesApi.markSettlementPaid(tripId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["settlement", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Marcado como pago. Aguardando confirmacao.");
    },
  });
}

export function useConfirmSettlement(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (recordId: string) => expensesApi.confirmSettlement(tripId, recordId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["settlement", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Quitacao confirmada.");
    },
  });
}
