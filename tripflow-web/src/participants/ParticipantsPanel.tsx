import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import type { TripRole } from "../api/types";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Input } from "../components/Input";
import { Spinner } from "../components/Spinner";
import { tripRoleLabels } from "../utils/labels";
import { useInviteParticipant, useParticipants, useRemoveParticipant } from "./hooks";

const statusLabels = { 0: "Convidado", 1: "Aceito", 2: "Recusado" } as const;

const inviteSchema = z.object({
  email: z.string().email("Informe um e-mail valido."),
  role: z.enum(["0", "1", "2"]),
});

type InviteFormValues = z.infer<typeof inviteSchema>;

export function ParticipantsPanel({ tripId, myRole }: { tripId: string; myRole: TripRole | undefined }) {
  const { data: participants, isLoading } = useParticipants(tripId);
  const inviteParticipant = useInviteParticipant(tripId);
  const removeParticipant = useRemoveParticipant(tripId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<InviteFormValues>({ resolver: zodResolver(inviteSchema), defaultValues: { role: "0" } });

  const canInvite = myRole === 1 || myRole === 2;
  const canRemove = myRole === 2;

  const onInvite = async (values: InviteFormValues) => {
    await inviteParticipant.mutateAsync({ email: values.email, role: Number(values.role) as TripRole });
    reset({ email: "", role: "0" });
  };

  if (isLoading) return <Spinner />;

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <h2 className="mb-4 text-lg font-medium text-slate-900">Participantes</h2>
        <ul className="flex flex-col divide-y divide-slate-100">
          {participants?.map((p) => (
            <li key={p.id} className="flex items-center justify-between py-3">
              <div>
                <p className="text-sm font-medium text-slate-800">{p.displayName ?? p.invitedEmail}</p>
                <p className="text-xs text-slate-500">
                  {statusLabels[p.status]} · {tripRoleLabels[p.role]}
                </p>
              </div>
              {canRemove && (
                <Button variant="ghost" onClick={() => removeParticipant.mutate(p.id)} disabled={removeParticipant.isPending}>
                  Remover
                </Button>
              )}
            </li>
          ))}
        </ul>
      </Card>

      {canInvite && (
        <Card>
          <h2 className="mb-4 text-lg font-medium text-slate-900">Convidar participante</h2>
          <form onSubmit={handleSubmit(onInvite)} className="flex flex-col gap-4 sm:flex-row sm:items-end">
            <div className="flex-1">
              <Input label="E-mail" type="email" error={errors.email?.message} {...register("email")} />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-slate-700" htmlFor="role">
                Papel
              </label>
              <select id="role" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" {...register("role")}>
                <option value="0">Visualizador</option>
                <option value="1">Editor</option>
                <option value="2">Dono</option>
              </select>
            </div>
            <Button type="submit" isLoading={isSubmitting}>
              Convidar
            </Button>
          </form>
          {inviteParticipant.isError && <div className="mt-3"><Alert message={getErrorMessage(inviteParticipant.error)} /></div>}
        </Card>
      )}
    </div>
  );
}
