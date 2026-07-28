import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { TripRole } from "../api/types";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Input } from "../components/Input";
import { Spinner } from "../components/Spinner";
import { useChecklist, useCreateChecklistItem, useDeleteChecklistItem, useToggleChecklistItem } from "./hooks";

const schema = z.object({ title: z.string().min(1, "Descreva o item.") });
type FormValues = z.infer<typeof schema>;

export function ChecklistPanel({ tripId, myRole }: { tripId: string; myRole: TripRole | undefined }) {
  const { data: items, isLoading } = useChecklist(tripId);
  const createItem = useCreateChecklistItem(tripId);
  const toggleItem = useToggleChecklistItem(tripId);
  const deleteItem = useDeleteChecklistItem(tripId);

  const canEdit = myRole === 1 || myRole === 2;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    await createItem.mutateAsync(values.title);
    reset();
  };

  if (isLoading) return <Spinner />;

  return (
    <Card>
      <h2 className="mb-4 text-lg font-medium text-slate-900">Checklist</h2>

      {canEdit && (
        <form onSubmit={handleSubmit(onSubmit)} className="mb-4 flex items-end gap-3">
          <div className="flex-1">
            <Input label="Novo item" error={errors.title?.message} {...register("title")} />
          </div>
          <Button type="submit" isLoading={isSubmitting}>
            Adicionar
          </Button>
        </form>
      )}

      {items?.length === 0 && <p className="text-sm text-slate-500">Nada na checklist ainda.</p>}

      <ul className="flex flex-col divide-y divide-slate-100">
        {items?.map((item) => (
          <li key={item.id} className="flex items-center justify-between py-3">
            <label className="flex items-center gap-3 text-sm">
              <input
                type="checkbox"
                checked={item.isDone}
                disabled={!canEdit || toggleItem.isPending}
                onChange={() => toggleItem.mutate(item)}
                className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
              />
              <span className={item.isDone ? "text-slate-400 line-through" : "text-slate-800"}>{item.title}</span>
            </label>
            {canEdit && (
              <Button variant="ghost" onClick={() => deleteItem.mutate(item.id)} disabled={deleteItem.isPending}>
                Remover
              </Button>
            )}
          </li>
        ))}
      </ul>
    </Card>
  );
}
