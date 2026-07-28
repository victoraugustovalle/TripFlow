import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "../auth/authStore";
import { ChecklistPanel } from "../checklist/ChecklistPanel";
import { Button } from "../components/Button";
import { Spinner } from "../components/Spinner";
import { ExpensesPanel } from "../expenses/ExpensesPanel";
import { ParticipantsPanel } from "../participants/ParticipantsPanel";
import { useParticipants } from "../participants/hooks";
import { useTripRealtime } from "../realtime/useTripRealtime";
import { tripStatusLabels } from "../utils/labels";
import { useDeleteTrip, useTrip } from "./hooks";

type Tab = "expenses" | "checklist" | "participants";

export function TripDetailPage() {
  const { tripId } = useParams<{ tripId: string }>();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const [tab, setTab] = useState<Tab>("expenses");

  const { data: trip, isLoading: isLoadingTrip } = useTrip(tripId!);
  const { data: participants, isLoading: isLoadingParticipants } = useParticipants(tripId!);
  const deleteTrip = useDeleteTrip();

  useTripRealtime(tripId!);

  if (isLoadingTrip || isLoadingParticipants) return <Spinner />;
  if (!trip || !participants) return <p className="text-sm text-red-600">Viagem nao encontrada.</p>;

  const myParticipant = participants.find((p) => p.userId === user?.id);
  const myRole = myParticipant?.role;

  const handleDelete = async () => {
    if (!confirm(`Apagar "${trip.name}"? Essa acao nao pode ser desfeita.`)) return;
    await deleteTrip.mutateAsync(trip.id);
    navigate("/");
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{trip.name}</h1>
          <p className="text-sm text-slate-500">
            {trip.destination ?? "Sem destino definido"} · {tripStatusLabels[trip.status]}
          </p>
        </div>
        {myRole === 2 && (
          <Button variant="danger" onClick={handleDelete} isLoading={deleteTrip.isPending}>
            Apagar viagem
          </Button>
        )}
      </div>

      <div className="flex gap-2 border-b border-slate-200">
        {(
          [
            ["expenses", "Gastos"],
            ["checklist", "Checklist"],
            ["participants", "Participantes"],
          ] as const
        ).map(([key, label]) => (
          <button
            key={key}
            onClick={() => setTab(key)}
            className={`border-b-2 px-4 py-2 text-sm font-medium transition-colors ${
              tab === key ? "border-brand-600 text-brand-700" : "border-transparent text-slate-500 hover:text-slate-700"
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {tab === "expenses" && (
        <ExpensesPanel tripId={trip.id} myRole={myRole} participants={participants} currency={trip.currency} />
      )}
      {tab === "checklist" && <ChecklistPanel tripId={trip.id} myRole={myRole} />}
      {tab === "participants" && <ParticipantsPanel tripId={trip.id} myRole={myRole} />}
    </div>
  );
}
