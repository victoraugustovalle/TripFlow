import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as expensesApi from "../api/expenses";
import type { CreateExpenseInput } from "../api/expenses";

export function useExpenses(tripId: string) {
  return useQuery({ queryKey: ["expenses", tripId], queryFn: () => expensesApi.listExpenses(tripId) });
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
    },
  });
}
