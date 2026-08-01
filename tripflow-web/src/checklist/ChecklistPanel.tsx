import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { ParticipantDto, TripRole } from "../api/types";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { Input } from "../components/Input";
import { SkeletonLines } from "../components/Skeleton";
import { formatDate } from "../utils/labels";
import { useChecklist, useCreateChecklistItem, useDeleteChecklistItem, useToggleChecklistItem } from "./hooks";

const schema = z.object({
  title: z.string().min(1, "Descreva o item."),
  assignedToParticipantId: z.string().optional(),
  dueDate: z.string().optional(),
});
type FormValues = z.infer<typeof schema>;

function participantName(participants: ParticipantDto[], participantId: string) {
  const participant = participants.find((p) => p.id === participantId);
  return participant?.displayName ?? participant?.invitedEmail ?? "Participante removido";
}

export function ChecklistPanel({
  tripId,
  myRole,
  participants,
}: {
  tripId: string;
  myRole: TripRole | undefined;
  participants: ParticipantDto[];
}) {
  const { data: items, isLoading } = useChecklist(tripId);
  const createItem = useCreateChecklistItem(tripId);
  const toggleItem = useToggleChecklistItem(tripId);
  const deleteItem = useDeleteChecklistItem(tripId);

  const canEdit = myRole === 1 || myRole === 2;
  const acceptedParticipants = participants.filter((p) => p.status === 1);
  const todayIso = new Date().toISOString().slice(0, 10);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    await createItem.mutateAsync({
      title: values.title,
      assignedToParticipantId: values.assignedToParticipantId || null,
      dueDate: values.dueDate || null,
    });
    reset();
  };

  if (isLoading) {
    return (
      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Checklist</h2>
        <SkeletonLines />
      </Card>
    );
  }

  return (
    <Card>
      <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Checklist</h2>

      {canEdit && (
        <form onSubmit={handleSubmit(onSubmit)} className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto_auto_auto] sm:items-end">
          <Input label="Novo item" error={errors.title?.message} {...register("title")} />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-navy-900" htmlFor="assignedToParticipantId">
              Responsavel (opcional)
            </label>
            <select
              id="assignedToParticipantId"
              className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
              {...register("assignedToParticipantId")}
            >
              <option value="">Ninguem</option>
              {acceptedParticipants.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.displayName ?? p.invitedEmail}
                </option>
              ))}
            </select>
          </div>
          <Input label="Prazo (opcional)" type="date" {...register("dueDate")} />
          <Button type="submit" isLoading={isSubmitting}>
            Adicionar
          </Button>
        </form>
      )}

      {items?.length === 0 && (
        <p className="flex items-center gap-2 text-sm text-navy-700/70">
          <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
          Nada na checklist ainda.
        </p>
      )}

      <ul className="flex flex-col divide-y divide-cream-200">
        {items?.map((item) => {
          const isOverdue = !item.isDone && item.dueDate != null && item.dueDate < todayIso;

          return (
            <li key={item.id} className="flex items-start justify-between gap-3 py-3">
              <label className="flex items-start gap-3 text-sm">
                <input
                  type="checkbox"
                  checked={item.isDone}
                  disabled={!canEdit || toggleItem.isPending}
                  onChange={() => toggleItem.mutate(item)}
                  className="mt-0.5 h-4 w-4 rounded border-cream-300 text-brand-600 focus:ring-brand-500"
                />
                <span className="flex flex-col gap-1">
                  <span className={`transition-colors duration-150 ${item.isDone ? "text-navy-700/50 line-through" : "text-navy-900"}`}>
                    {item.title}
                  </span>
                  {(item.assignedToParticipantId || item.dueDate) && (
                    <span className="flex flex-wrap items-center gap-x-2 gap-y-1">
                      {item.assignedToParticipantId && (
                        <Badge tone="neutral">{participantName(participants, item.assignedToParticipantId)}</Badge>
                      )}
                      {item.dueDate && (
                        <span className={`text-xs ${isOverdue ? "font-medium text-coral-700" : "text-navy-700/50"}`}>
                          Prazo: {formatDate(item.dueDate)}
                          {isOverdue && " (atrasado)"}
                        </span>
                      )}
                    </span>
                  )}
                </span>
              </label>
              {canEdit && (
                <Button variant="ghostDanger" onClick={() => deleteItem.mutate(item.id)} disabled={deleteItem.isPending}>
                  Remover
                </Button>
              )}
            </li>
          );
        })}
      </ul>
    </Card>
  );
}
