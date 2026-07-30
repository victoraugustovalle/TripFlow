import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import type { TripRole } from "../api/types";
import { Alert } from "../components/Alert";
import { Avatar } from "../components/Avatar";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Input } from "../components/Input";
import { SkeletonLines } from "../components/Skeleton";
import { participantStatusLabels, participantStatusTone, tripRoleLabels } from "../utils/labels";
import { useInviteParticipant, useParticipants, useRemoveParticipant } from "./hooks";

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

  if (isLoading) {
    return (
      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Participantes</h2>
        <SkeletonLines />
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Participantes</h2>
        <ul className="flex flex-col divide-y divide-cream-200">
          {participants?.map((p) => (
            <li key={p.id} className="flex items-center gap-3 py-3">
              <Avatar name={p.displayName ?? p.invitedEmail} />
              <div className="flex-1">
                <p className="text-sm font-medium text-navy-900">{p.displayName ?? p.invitedEmail}</p>
                {p.displayName && <p className="text-xs text-navy-700/50">{p.invitedEmail}</p>}
                <div className="mt-1 flex items-center gap-2">
                  <Badge tone={participantStatusTone[p.status]}>{participantStatusLabels[p.status]}</Badge>
                  <span className="text-xs text-navy-700/50">{tripRoleLabels[p.role]}</span>
                </div>
              </div>
              {canRemove && (
                <Button variant="ghostDanger" onClick={() => removeParticipant.mutate(p.id)} disabled={removeParticipant.isPending}>
                  Remover
                </Button>
              )}
            </li>
          ))}
        </ul>
      </Card>

      {canInvite && (
        <Card>
          <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Convidar participante</h2>
          <form onSubmit={handleSubmit(onInvite)} className="flex flex-col gap-4 sm:flex-row sm:items-end">
            <div className="flex-1">
              <Input label="E-mail" type="email" error={errors.email?.message} {...register("email")} />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-navy-900" htmlFor="role">
                Papel
              </label>
              <select
                id="role"
                className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                  transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
                {...register("role")}
              >
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
