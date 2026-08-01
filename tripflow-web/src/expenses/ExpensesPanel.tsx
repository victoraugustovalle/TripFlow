import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import type { ParticipantDto, TripRole } from "../api/types";
import { useAuthStore } from "../auth/authStore";
import { BudgetSection } from "../budgets/BudgetSection";
import { useBudgets } from "../budgets/hooks";
import type { SettlementTransferDto } from "../api/types";
import { Alert } from "../components/Alert";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { Input } from "../components/Input";
import { SkeletonLines } from "../components/Skeleton";
import { formatCurrency, formatDate } from "../utils/labels";
import { useExpenseDraftStore } from "./expenseDraftStore";
import {
  useConfirmSettlement,
  useCreateExpense,
  useDeleteExpense,
  useExpenses,
  useMarkSettlementPaid,
  useSettlement,
} from "./hooks";

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 400;

const schema = z.object({
  description: z.string().min(1, "Descreva o gasto."),
  amount: z.coerce.number().positive("Informe um valor maior que zero."),
  category: z.string().min(1, "Informe a categoria."),
  expenseDate: z.string().min(1, "Informe a data."),
  paidByParticipantId: z.string().min(1, "Informe quem pagou."),
});

// z.coerce.number() faz o tipo de entrada do formulario (antes de validar) divergir do
// tipo de saida (depois de coagir) - RHF precisa dos dois separados nesse caso.
type FormInput = z.input<typeof schema>;
type FormOutput = z.output<typeof schema>;

function participantName(participants: ParticipantDto[], participantId: string) {
  const participant = participants.find((p) => p.id === participantId);
  return participant?.displayName ?? participant?.invitedEmail ?? "Participante removido";
}

/** Ponto de partida da divisao customizada: igual pra todo mundo, com o resto de centavos
 * indo pros primeiros da lista - mesmo criterio do SplitEqually do backend, so que aqui e
 * so um valor inicial que o usuario pode reajustar. */
function distributeEqually(amount: number, participantIds: string[]): Record<string, string> {
  if (participantIds.length === 0) return {};
  const totalCents = Math.round(amount * 100);
  const baseCents = Math.floor(totalCents / participantIds.length);
  const remainderCents = totalCents - baseCents * participantIds.length;

  const shares: Record<string, string> = {};
  participantIds.forEach((id, index) => {
    const cents = baseCents + (index < remainderCents ? 1 : 0);
    shares[id] = (cents / 100).toFixed(2);
  });
  return shares;
}

export function ExpensesPanel({
  tripId,
  myRole,
  participants,
  currency,
}: {
  tripId: string;
  myRole: TripRole | undefined;
  participants: ParticipantDto[];
  currency: string;
}) {
  const { data: settlement } = useSettlement(tripId);
  const { data: budgets } = useBudgets(tripId);
  const createExpense = useCreateExpense(tripId);
  const deleteExpense = useDeleteExpense(tripId);
  const markSettlementPaid = useMarkSettlementPaid(tripId);
  const confirmSettlement = useConfirmSettlement(tripId);
  const currentUser = useAuthStore((s) => s.user);
  const myParticipantId = participants.find((p) => p.userId === currentUser?.id)?.id;

  const findPendingSettlement = (transfer: SettlementTransferDto) =>
    settlement?.pendingSettlements.find(
      (r) => r.fromParticipantId === transfer.fromParticipantId && r.toParticipantId === transfer.toParticipantId,
    );

  // Le uma vez no mount (vindo de "Lancar como gasto" numa reserva) e limpa em seguida - o
  // ExpensesPanel desmonta/remonta a cada troca de aba, entao isso nunca fica "grudado".
  const expenseDraft = useExpenseDraftStore((s) => s.draft);
  const clearExpenseDraft = useExpenseDraftStore((s) => s.clearDraft);
  useEffect(() => {
    if (expenseDraft) clearExpenseDraft();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const canEdit = myRole === 1 || myRole === 2;
  const acceptedParticipants = participants.filter((p) => p.status === 1);
  const categoryOptions = budgets?.map((b) => b.category) ?? [];

  const [splitMode, setSplitMode] = useState<"equal" | "custom">("equal");
  const [customShares, setCustomShares] = useState<Record<string, string>>({});

  const [searchInput, setSearchInput] = useState("");
  const [filters, setFilters] = useState({ category: "", from: "", to: "", search: "" });
  const [page, setPage] = useState(1);

  // Debounce da busca livre - senao cada tecla digitada dispara uma chamada nova.
  useEffect(() => {
    const timeout = setTimeout(() => {
      setFilters((prev) => ({ ...prev, search: searchInput.trim() }));
      setPage(1);
    }, SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  const updateFilter = (patch: Partial<typeof filters>) => {
    setFilters((prev) => ({ ...prev, ...patch }));
    setPage(1);
  };

  const clearFilters = () => {
    setFilters({ category: "", from: "", to: "", search: "" });
    setSearchInput("");
    setPage(1);
  };

  const hasActiveFilters = Boolean(filters.category || filters.from || filters.to || filters.search);

  const {
    data: expensesPage,
    isLoading,
    isPlaceholderData,
  } = useExpenses(tripId, {
    category: filters.category || undefined,
    from: filters.from || undefined,
    to: filters.to || undefined,
    search: filters.search || undefined,
    page,
    pageSize: PAGE_SIZE,
  });

  const totalPages = expensesPage ? Math.max(1, Math.ceil(expensesPage.totalCount / expensesPage.pageSize)) : 1;

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormInput, unknown, FormOutput>({
    resolver: zodResolver(schema),
    defaultValues: expenseDraft
      ? { ...expenseDraft }
      : { expenseDate: new Date().toISOString().slice(0, 10), category: "Geral" },
  });

  const amountValue = Number(watch("amount")) || 0;
  const customSharesTotal = Object.values(customShares).reduce((sum, value) => sum + (Number(value) || 0), 0);
  const customSplitMismatch = splitMode === "custom" && Math.abs(customSharesTotal - amountValue) > 0.01;

  const enableCustomSplit = () => {
    setSplitMode("custom");
    setCustomShares(distributeEqually(amountValue, acceptedParticipants.map((p) => p.id)));
  };

  const onSubmit = async (values: FormOutput) => {
    const shares =
      splitMode === "custom"
        ? acceptedParticipants
            .map((p) => ({ participantId: p.id, shareAmount: Number(customShares[p.id] ?? 0) }))
            .filter((s) => s.shareAmount > 0)
        : null;

    await createExpense.mutateAsync({
      description: values.description,
      amount: values.amount,
      category: values.category,
      expenseDate: values.expenseDate,
      paidByParticipantId: values.paidByParticipantId,
      splitBetweenParticipantIds: null,
      shares,
    });
    reset({ description: "", amount: 0, category: "Geral", expenseDate: values.expenseDate, paidByParticipantId: values.paidByParticipantId });
    setSplitMode("equal");
    setCustomShares({});
  };

  if (isLoading) {
    return (
      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Gastos</h2>
        <SkeletonLines />
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      {canEdit && (
        <Card>
          <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Novo gasto</h2>
          <form onSubmit={handleSubmit(onSubmit)} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input label="Descricao" error={errors.description?.message} {...register("description")} />
            <Input label="Valor" type="number" step="0.01" error={errors.amount?.message} {...register("amount")} />
            <div>
              <Input
                label="Categoria"
                list="category-suggestions"
                autoComplete="off"
                error={errors.category?.message}
                {...register("category")}
              />
              <datalist id="category-suggestions">
                {categoryOptions.map((category) => (
                  <option key={category} value={category} />
                ))}
              </datalist>
            </div>
            <Input label="Data" type="date" error={errors.expenseDate?.message} {...register("expenseDate")} />
            <div className="flex flex-col gap-1 sm:col-span-2">
              <label className="text-sm font-medium text-navy-900" htmlFor="paidBy">
                Quem pagou
              </label>
              <select
                id="paidBy"
                className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                  transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
                {...register("paidByParticipantId")}
              >
                <option value="">Selecione...</option>
                {acceptedParticipants.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.displayName ?? p.invitedEmail}
                  </option>
                ))}
              </select>
              {errors.paidByParticipantId && <span className="text-sm text-coral-700">{errors.paidByParticipantId.message}</span>}
            </div>

            <div className="flex flex-col gap-2 sm:col-span-2">
              <span className="text-sm font-medium text-navy-900">Divisao</span>
              <div className="flex w-fit gap-1 rounded-full border border-cream-300 p-1">
                <button
                  type="button"
                  onClick={() => setSplitMode("equal")}
                  className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
                    splitMode === "equal" ? "bg-brand-700 text-white" : "text-navy-700/70 hover:bg-cream-100"
                  }`}
                >
                  Dividir igualmente
                </button>
                <button
                  type="button"
                  onClick={enableCustomSplit}
                  className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
                    splitMode === "custom" ? "bg-brand-700 text-white" : "text-navy-700/70 hover:bg-cream-100"
                  }`}
                >
                  Valores customizados
                </button>
              </div>

              {splitMode === "equal" && (
                <p className="text-xs text-navy-700/50">Divide igualmente entre todos os participantes aceitos.</p>
              )}

              {splitMode === "custom" && (
                <div className="flex flex-col gap-2 rounded-lg border border-cream-300 p-3">
                  {acceptedParticipants.map((p) => (
                    <div key={p.id} className="flex items-center justify-between gap-3">
                      <span className="text-sm text-navy-900">{p.displayName ?? p.invitedEmail}</span>
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        value={customShares[p.id] ?? ""}
                        onChange={(e) => setCustomShares((prev) => ({ ...prev, [p.id]: e.target.value }))}
                        className="w-28 rounded-lg border border-cream-300 px-3 py-1.5 text-sm text-navy-900 shadow-sm outline-none
                          focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
                      />
                    </div>
                  ))}
                  <div
                    className={`flex items-center justify-between border-t border-cream-200 pt-2 text-sm font-medium ${
                      customSplitMismatch ? "text-coral-700" : "text-brand-700"
                    }`}
                  >
                    <span>Soma</span>
                    <span>
                      {formatCurrency(customSharesTotal, currency)} de {formatCurrency(amountValue, currency)}
                    </span>
                  </div>
                  {customSplitMismatch && (
                    <p className="text-xs text-coral-700">A soma precisa ser igual ao valor do gasto pra poder salvar.</p>
                  )}
                </div>
              )}
            </div>

            {createExpense.isError && (
              <div className="sm:col-span-2">
                <Alert message={getErrorMessage(createExpense.error)} />
              </div>
            )}

            <Button type="submit" isLoading={isSubmitting} disabled={customSplitMismatch} className="sm:col-span-2">
              Adicionar gasto
            </Button>
          </form>
        </Card>
      )}

      <BudgetSection tripId={tripId} myRole={myRole} currency={currency} />

      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
          <h2 className="font-display text-lg font-medium text-navy-900">Gastos</h2>
          {hasActiveFilters && (
            <button type="button" onClick={clearFilters} className="text-xs font-medium text-brand-700 hover:underline">
              Limpar filtros
            </button>
          )}
        </div>

        <div className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-4">
          <input
            type="text"
            placeholder="Buscar por descricao ou categoria"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
              focus:border-brand-500 focus:ring-1 focus:ring-brand-500 sm:col-span-2"
          />
          <select
            value={filters.category}
            onChange={(e) => updateFilter({ category: e.target.value })}
            className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
              focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
          >
            <option value="">Todas as categorias</option>
            {categoryOptions.map((category) => (
              <option key={category} value={category}>
                {category}
              </option>
            ))}
          </select>
          <div className="grid grid-cols-2 gap-2">
            <input
              type="date"
              value={filters.from}
              onChange={(e) => updateFilter({ from: e.target.value })}
              className="rounded-lg border border-cream-300 px-2 py-2 text-sm text-navy-900 shadow-sm outline-none
                focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
            />
            <input
              type="date"
              value={filters.to}
              onChange={(e) => updateFilter({ to: e.target.value })}
              className="rounded-lg border border-cream-300 px-2 py-2 text-sm text-navy-900 shadow-sm outline-none
                focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
            />
          </div>
        </div>

        {expensesPage?.items.length === 0 && (
          <p className="flex items-center gap-2 text-sm text-navy-700/70">
            <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
            {hasActiveFilters ? "Nenhum gasto encontrado com esses filtros." : "Nenhum gasto lancado ainda."}
          </p>
        )}

        <ul className={`flex flex-col divide-y divide-cream-200 transition-opacity ${isPlaceholderData ? "opacity-50" : ""}`}>
          {expensesPage?.items.map((expense) => (
            <li key={expense.id} className="flex items-center justify-between py-3">
              <div>
                <p className="text-sm font-medium text-navy-900">{expense.description}</p>
                <p className="text-xs text-navy-700/50">
                  {expense.category} · {formatDate(expense.expenseDate)} · pago por{" "}
                  {participantName(participants, expense.paidByParticipantId)}
                </p>
              </div>
              <div className="flex items-center gap-3">
                <span className="text-sm font-medium text-navy-900">{formatCurrency(expense.amount, currency)}</span>
                {canEdit && (
                  <Button variant="ghostDanger" onClick={() => deleteExpense.mutate(expense.id)} disabled={deleteExpense.isPending}>
                    Remover
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>

        {expensesPage && expensesPage.totalCount > 0 && (
          <div className="mt-4 flex items-center justify-between border-t border-cream-200 pt-3 text-sm">
            <span className="text-navy-700/70">
              {expensesPage.totalCount} {expensesPage.totalCount === 1 ? "gasto" : "gastos"}
            </span>
            <div className="flex items-center gap-3">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="font-medium text-brand-700 hover:underline disabled:cursor-not-allowed disabled:text-navy-700/30 disabled:hover:no-underline"
              >
                Anterior
              </button>
              <span className="text-navy-700/70">
                Pagina {page} de {totalPages}
              </span>
              <button
                type="button"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="font-medium text-brand-700 hover:underline disabled:cursor-not-allowed disabled:text-navy-700/30 disabled:hover:no-underline"
              >
                Proxima
              </button>
            </div>
          </div>
        )}
      </Card>

      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Quem deve pra quem</h2>
        {settlement?.transfers.length === 0 && <p className="text-sm text-navy-700/70">Nada pendente - contas zeradas.</p>}
        <ul className="flex flex-col gap-2">
          {settlement?.transfers.map((transfer, index) => {
            const iOwe = myParticipantId != null && transfer.fromParticipantId === myParticipantId;
            const owedToMe = myParticipantId != null && transfer.toParticipantId === myParticipantId;
            const pendingRecord = findPendingSettlement(transfer);
            return (
              <li
                key={index}
                className={`flex flex-wrap items-center justify-between gap-3 rounded-lg px-3 py-2 text-sm ${
                  iOwe
                    ? "bg-coral-50 font-medium text-navy-900"
                    : owedToMe
                      ? "bg-brand-50 font-medium text-navy-900"
                      : "text-navy-700/70"
                }`}
              >
                <span>
                  <strong>{participantName(participants, transfer.fromParticipantId)}</strong> deve pagar{" "}
                  <strong>{formatCurrency(transfer.amount, currency)}</strong> pra{" "}
                  <strong>{participantName(participants, transfer.toParticipantId)}</strong>
                </span>

                {pendingRecord ? (
                  owedToMe ? (
                    <Button
                      variant="secondary"
                      onClick={() => confirmSettlement.mutate(pendingRecord.id)}
                      isLoading={confirmSettlement.isPending}
                    >
                      Confirmar recebimento
                    </Button>
                  ) : (
                    <Badge tone="warning">Aguardando confirmacao</Badge>
                  )
                ) : (
                  iOwe && (
                    <Button
                      variant="secondary"
                      onClick={() => markSettlementPaid.mutate({ toParticipantId: transfer.toParticipantId, amount: transfer.amount })}
                      isLoading={markSettlementPaid.isPending}
                    >
                      Marcar como pago
                    </Button>
                  )
                )}
              </li>
            );
          })}
        </ul>
      </Card>
    </div>
  );
}
