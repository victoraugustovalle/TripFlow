import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import type { TripRole } from "../api/types";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { Input } from "../components/Input";
import { SkeletonLines } from "../components/Skeleton";
import { formatCurrency } from "../utils/labels";
import { useBudgets, useDeleteBudget, useUpsertBudget } from "./hooks";

const EMPTY_ID = "00000000-0000-0000-0000-000000000000";

const schema = z.object({
  category: z.string().min(1, "Informe a categoria."),
  plannedAmount: z.coerce.number().nonnegative("Informe um valor valido."),
});

type FormInput = z.input<typeof schema>;
type FormOutput = z.output<typeof schema>;

function barTone(actual: number, planned: number) {
  if (planned <= 0) return "bg-navy-700/30";
  return actual > planned ? "bg-coral-600" : "bg-brand-600";
}

export function BudgetSection({ tripId, myRole, currency }: { tripId: string; myRole: TripRole | undefined; currency: string }) {
  const { data: budgets, isLoading } = useBudgets(tripId);
  const upsertBudget = useUpsertBudget(tripId);
  const deleteBudget = useDeleteBudget(tripId);
  const canEdit = myRole === 1 || myRole === 2;

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormInput, unknown, FormOutput>({
    resolver: zodResolver(schema),
    defaultValues: { category: "", plannedAmount: 0 },
  });

  // Categorias que ja tem gasto lancado mas nenhum orcamento definido (BudgetDto.id vazio) -
  // uteis pra sugerir no formulario, sem forcar o usuario a redigitar o nome exato.
  const categoriesWithoutBudget = (budgets ?? []).filter((b) => b.id === EMPTY_ID);

  useEffect(() => {
    reset({ category: "", plannedAmount: 0 });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tripId]);

  const onSubmit = async (values: FormOutput) => {
    await upsertBudget.mutateAsync({ category: values.category, plannedAmount: values.plannedAmount });
    reset({ category: "", plannedAmount: 0 });
  };

  const startEdit = (category: string, plannedAmount: number) => {
    setValue("category", category);
    setValue("plannedAmount", plannedAmount);
  };

  if (isLoading) {
    return (
      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Orcamento por categoria</h2>
        <SkeletonLines />
      </Card>
    );
  }

  return (
    <Card>
      <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Orcamento por categoria</h2>

      {budgets?.length === 0 && (
        <p className="mb-4 flex items-center gap-2 text-sm text-navy-700/70">
          <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
          Nenhuma categoria ainda - lance um gasto ou defina um planejado abaixo.
        </p>
      )}

      {budgets && budgets.length > 0 && (
        <ul className="mb-5 flex flex-col gap-3">
          {budgets.map((budget) => (
            <li key={budget.category}>
              <div className="mb-1 flex items-baseline justify-between text-sm">
                <span className="font-medium text-navy-900">{budget.category}</span>
                <span className="flex items-center gap-2 text-navy-700/70">
                  {formatCurrency(budget.actualAmount, currency)}
                  {budget.plannedAmount > 0 && ` / ${formatCurrency(budget.plannedAmount, currency)}`}
                  {canEdit && (
                    <>
                      <button
                        type="button"
                        onClick={() => startEdit(budget.category, budget.plannedAmount)}
                        className="text-xs font-medium text-brand-700 hover:underline"
                      >
                        Editar
                      </button>
                      {budget.id !== EMPTY_ID && (
                        <Button
                          variant="ghostDanger"
                          onClick={() => deleteBudget.mutate(budget.id)}
                          disabled={deleteBudget.isPending}
                        >
                          Remover
                        </Button>
                      )}
                    </>
                  )}
                </span>
              </div>
              <div className="h-1.5 w-full overflow-hidden rounded-full bg-cream-200">
                <div
                  className={`h-full rounded-full ${barTone(budget.actualAmount, budget.plannedAmount)}`}
                  style={{
                    width: `${budget.plannedAmount > 0 ? Math.min(100, (budget.actualAmount / budget.plannedAmount) * 100) : 100}%`,
                  }}
                />
              </div>
            </li>
          ))}
        </ul>
      )}

      {canEdit && (
        <form onSubmit={handleSubmit(onSubmit)} className="grid grid-cols-1 gap-4 border-t border-cream-200 pt-4 sm:grid-cols-[1fr_auto_auto]">
          <Input
            label="Categoria"
            list="budget-categories"
            error={errors.category?.message}
            {...register("category")}
          />
          <datalist id="budget-categories">
            {categoriesWithoutBudget.map((b) => (
              <option key={b.category} value={b.category} />
            ))}
          </datalist>
          <Input label="Valor planejado" type="number" step="0.01" min="0" error={errors.plannedAmount?.message} {...register("plannedAmount")} />
          <div className="flex items-end">
            <Button type="submit" isLoading={isSubmitting} className="w-full sm:w-auto">
              Definir
            </Button>
          </div>

          {upsertBudget.isError && (
            <div className="sm:col-span-3">
              <Alert message={getErrorMessage(upsertBudget.error)} />
            </div>
          )}
        </form>
      )}
    </Card>
  );
}
