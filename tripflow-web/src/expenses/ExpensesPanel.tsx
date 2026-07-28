import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import type { ParticipantDto, TripRole } from "../api/types";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Input } from "../components/Input";
import { Spinner } from "../components/Spinner";
import { formatCurrency, formatDate } from "../utils/labels";
import { useCreateExpense, useDeleteExpense, useExpenses, useSettlement } from "./hooks";

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
  const { data: expenses, isLoading } = useExpenses(tripId);
  const { data: settlement } = useSettlement(tripId);
  const createExpense = useCreateExpense(tripId);
  const deleteExpense = useDeleteExpense(tripId);

  const canEdit = myRole === 1 || myRole === 2;
  const acceptedParticipants = participants.filter((p) => p.status === 1);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormInput, unknown, FormOutput>({
    resolver: zodResolver(schema),
    defaultValues: { expenseDate: new Date().toISOString().slice(0, 10), category: "Geral" },
  });

  const onSubmit = async (values: FormOutput) => {
    await createExpense.mutateAsync({
      description: values.description,
      amount: values.amount,
      category: values.category,
      expenseDate: values.expenseDate,
      paidByParticipantId: values.paidByParticipantId,
      splitBetweenParticipantIds: null,
    });
    reset({ description: "", amount: 0, category: "Geral", expenseDate: values.expenseDate, paidByParticipantId: values.paidByParticipantId });
  };

  if (isLoading) return <Spinner />;

  return (
    <div className="flex flex-col gap-6">
      {canEdit && (
        <Card>
          <h2 className="mb-4 text-lg font-medium text-slate-900">Novo gasto</h2>
          <form onSubmit={handleSubmit(onSubmit)} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input label="Descricao" error={errors.description?.message} {...register("description")} />
            <Input label="Valor" type="number" step="0.01" error={errors.amount?.message} {...register("amount")} />
            <Input label="Categoria" error={errors.category?.message} {...register("category")} />
            <Input label="Data" type="date" error={errors.expenseDate?.message} {...register("expenseDate")} />
            <div className="flex flex-col gap-1 sm:col-span-2">
              <label className="text-sm font-medium text-slate-700" htmlFor="paidBy">
                Quem pagou
              </label>
              <select id="paidBy" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" {...register("paidByParticipantId")}>
                <option value="">Selecione...</option>
                {acceptedParticipants.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.displayName ?? p.invitedEmail}
                  </option>
                ))}
              </select>
              {errors.paidByParticipantId && <span className="text-sm text-red-600">{errors.paidByParticipantId.message}</span>}
            </div>

            {createExpense.isError && (
              <div className="sm:col-span-2">
                <Alert message={getErrorMessage(createExpense.error)} />
              </div>
            )}

            <Button type="submit" isLoading={isSubmitting} className="sm:col-span-2">
              Adicionar gasto
            </Button>
          </form>
        </Card>
      )}

      <Card>
        <h2 className="mb-4 text-lg font-medium text-slate-900">Gastos</h2>
        {expenses?.length === 0 && <p className="text-sm text-slate-500">Nenhum gasto lancado ainda.</p>}
        <ul className="flex flex-col divide-y divide-slate-100">
          {expenses?.map((expense) => (
            <li key={expense.id} className="flex items-center justify-between py-3">
              <div>
                <p className="text-sm font-medium text-slate-800">{expense.description}</p>
                <p className="text-xs text-slate-500">
                  {expense.category} · {formatDate(expense.expenseDate)} · pago por{" "}
                  {participantName(participants, expense.paidByParticipantId)}
                </p>
              </div>
              <div className="flex items-center gap-3">
                <span className="text-sm font-medium text-slate-900">{formatCurrency(expense.amount, currency)}</span>
                {canEdit && (
                  <Button variant="ghost" onClick={() => deleteExpense.mutate(expense.id)} disabled={deleteExpense.isPending}>
                    Remover
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>
      </Card>

      <Card>
        <h2 className="mb-4 text-lg font-medium text-slate-900">Quem deve pra quem</h2>
        {settlement?.transfers.length === 0 && <p className="text-sm text-slate-500">Nada pendente - contas zeradas.</p>}
        <ul className="flex flex-col gap-2">
          {settlement?.transfers.map((transfer, index) => (
            <li key={index} className="text-sm text-slate-700">
              <strong>{participantName(participants, transfer.fromParticipantId)}</strong> deve pagar{" "}
              <strong>{formatCurrency(transfer.amount, currency)}</strong> pra{" "}
              <strong>{participantName(participants, transfer.toParticipantId)}</strong>
            </li>
          ))}
        </ul>
      </Card>
    </div>
  );
}
