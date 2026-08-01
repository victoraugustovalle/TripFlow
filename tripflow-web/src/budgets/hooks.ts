import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as budgetsApi from "../api/budgets";
import { pushToast } from "../toast/toastStore";

export function useBudgets(tripId: string) {
  return useQuery({ queryKey: ["budgets", tripId], queryFn: () => budgetsApi.listBudgets(tripId) });
}

export function useUpsertBudget(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ category, plannedAmount }: { category: string; plannedAmount: number }) =>
      budgetsApi.upsertBudget(tripId, category, plannedAmount),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Orcamento atualizado.");
    },
  });
}

export function useDeleteBudget(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (budgetId: string) => budgetsApi.deleteBudget(tripId, budgetId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Orcamento removido.");
    },
  });
}
