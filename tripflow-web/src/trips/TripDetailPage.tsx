import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "../auth/authStore";
import { ChecklistPanel } from "../checklist/ChecklistPanel";
import { BackLink } from "../components/BackLink";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { CoverPickerModal } from "../components/CoverPickerModal";
import { FlightTrail } from "../components/FlightTrail";
import { Input } from "../components/Input";
import { Modal } from "../components/Modal";
import { Spinner } from "../components/Spinner";
import { ExpensesPanel } from "../expenses/ExpensesPanel";
import { ItineraryPanel } from "../itinerary/ItineraryPanel";
import { ParticipantsPanel } from "../participants/ParticipantsPanel";
import { useParticipants } from "../participants/hooks";
import { useTripRealtime } from "../realtime/useTripRealtime";
import { pushToast } from "../toast/toastStore";
import { tripStatusLabels, tripStatusTone } from "../utils/labels";
import { useDeleteTrip, useTrip, useUpdateTrip } from "./hooks";

function PencilIcon() {
  return (
    <svg viewBox="0 0 16 16" className="h-4 w-4" fill="none" aria-hidden="true">
      <path
        d="M11.5 2.5a1.5 1.5 0 0 1 2 2L5 13l-3 1 1-3 8.5-8.5Z"
        stroke="currentColor"
        strokeWidth="1.3"
        strokeLinejoin="round"
      />
    </svg>
  );
}

type Tab = "expenses" | "checklist" | "itinerary" | "participants";

export function TripDetailPage() {
  const { tripId } = useParams<{ tripId: string }>();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const [tab, setTab] = useState<Tab>("expenses");
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [confirmName, setConfirmName] = useState("");
  const [isEditingName, setIsEditingName] = useState(false);
  const [nameDraft, setNameDraft] = useState("");
  const [isCoverModalOpen, setIsCoverModalOpen] = useState(false);

  const { data: trip, isLoading: isLoadingTrip } = useTrip(tripId!);
  const { data: participants, isLoading: isLoadingParticipants } = useParticipants(tripId!);
  const deleteTrip = useDeleteTrip();
  const updateTrip = useUpdateTrip(tripId!);

  useTripRealtime(tripId!);

  if (isLoadingTrip || isLoadingParticipants) return <Spinner />;
  if (!trip || !participants) return <p className="text-sm text-coral-700">Viagem nao encontrada.</p>;

  const myParticipant = participants.find((p) => p.userId === user?.id);
  const myRole = myParticipant?.role;
  const canEditName = myRole === 1 || myRole === 2;

  const startEditName = () => {
    setNameDraft(trip.name);
    setIsEditingName(true);
  };

  const cancelEditName = () => setIsEditingName(false);

  const handleSaveName = async () => {
    const trimmed = nameDraft.trim();
    if (!trimmed) return;
    await updateTrip.mutateAsync({
      name: trimmed,
      destination: trip.destination,
      description: trip.description,
      startDate: trip.startDate,
      endDate: trip.endDate,
      status: trip.status,
      coverImageUrl: trip.coverImageUrl,
    });
    setIsEditingName(false);
    pushToast("Nome da viagem atualizado.");
  };

  const handleSelectCover = async (coverImageUrl: string) => {
    await updateTrip.mutateAsync({
      name: trip.name,
      destination: trip.destination,
      description: trip.description,
      startDate: trip.startDate,
      endDate: trip.endDate,
      status: trip.status,
      coverImageUrl,
    });
    setIsCoverModalOpen(false);
    pushToast("Capa da viagem atualizada.");
  };

  const closeDeleteModal = () => {
    setIsDeleteModalOpen(false);
    setConfirmName("");
  };

  const handleDelete = async () => {
    await deleteTrip.mutateAsync(trip.id);
    navigate("/");
  };

  return (
    <div className="flex flex-col gap-6">
      <BackLink to="/" label="Voltar pras viagens" />

      <div className="relative h-40 overflow-hidden rounded-2xl sm:h-48">
        {trip.coverImageUrl ? (
          <img src={trip.coverImageUrl} alt="" className="h-full w-full object-cover" />
        ) : (
          <div className="h-full w-full bg-gradient-to-br from-brand-500 to-brand-700">
            <FlightTrail className="h-full w-full text-cream-100/40" />
          </div>
        )}
        {canEditName && (
          <button
            type="button"
            onClick={() => setIsCoverModalOpen(true)}
            className="absolute bottom-3 right-3 rounded-full bg-white/90 px-3 py-1.5 text-xs font-semibold text-brand-700
              shadow-sm transition-colors hover:bg-white"
          >
            {trip.coverImageUrl ? "Trocar capa" : "Adicionar capa"}
          </button>
        )}
      </div>

      {isCoverModalOpen && (
        <CoverPickerModal onSelect={handleSelectCover} onClose={() => setIsCoverModalOpen(false)} />
      )}

      <div className="flex items-start justify-between">
        <div>
          {isEditingName ? (
            <form
              onSubmit={(e) => {
                e.preventDefault();
                void handleSaveName();
              }}
              className="flex flex-wrap items-center gap-2"
            >
              <input
                value={nameDraft}
                onChange={(e) => setNameDraft(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Escape") cancelEditName();
                }}
                autoFocus
                className="font-display rounded-lg border border-cream-300 bg-white px-2 py-1 text-2xl font-semibold text-navy-900
                  shadow-sm outline-none transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
              />
              <Button type="submit" isLoading={updateTrip.isPending} disabled={!nameDraft.trim()}>
                Salvar
              </Button>
              <Button type="button" variant="secondary" onClick={cancelEditName}>
                Cancelar
              </Button>
            </form>
          ) : (
            <div className="flex items-center gap-1.5">
              <h1 className="font-display text-2xl font-semibold text-navy-900">{trip.name}</h1>
              {canEditName && (
                <button
                  type="button"
                  onClick={startEditName}
                  aria-label="Editar nome da viagem"
                  className="rounded-full p-1.5 text-navy-700/50 transition-colors hover:bg-cream-200 hover:text-navy-900"
                >
                  <PencilIcon />
                </button>
              )}
            </div>
          )}
          <div className="mt-1 flex items-center gap-2">
            <p className="text-sm text-navy-700/70">{trip.destination ?? "Sem destino definido"}</p>
            <Badge tone={tripStatusTone[trip.status]}>{tripStatusLabels[trip.status]}</Badge>
          </div>
        </div>
        {myRole === 2 && (
          <Button variant="danger" onClick={() => setIsDeleteModalOpen(true)}>
            Apagar viagem
          </Button>
        )}
      </div>

      {isDeleteModalOpen && (
        <Modal title="Apagar viagem" onClose={closeDeleteModal}>
          <p className="text-sm text-navy-700/70">
            Essa acao nao pode ser desfeita. Todos os gastos, o checklist e o roteiro serao apagados junto. Pra confirmar,
            digite <strong className="text-navy-900">{trip.name}</strong> abaixo.
          </p>
          <div className="mt-4">
            <Input
              label="Nome da viagem"
              value={confirmName}
              onChange={(e) => setConfirmName(e.target.value)}
              autoFocus
              autoComplete="off"
            />
          </div>
          <div className="mt-5 flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={closeDeleteModal}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant="danger"
              onClick={handleDelete}
              isLoading={deleteTrip.isPending}
              disabled={confirmName !== trip.name}
            >
              Apagar viagem
            </Button>
          </div>
        </Modal>
      )}

      <div className="overflow-x-auto">
        <div className="flex w-fit gap-1 rounded-full border border-cream-300 p-1">
          {(
            [
              ["expenses", "Gastos"],
              ["checklist", "Checklist"],
              ["itinerary", "Roteiro"],
              ["participants", "Participantes"],
            ] as const
          ).map(([key, label]) => (
            <button
              key={key}
              onClick={() => setTab(key)}
              className={`shrink-0 rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
                tab === key ? "bg-brand-700 text-white" : "text-navy-700/70 hover:bg-cream-100"
              }`}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      {tab === "expenses" && (
        <ExpensesPanel tripId={trip.id} myRole={myRole} participants={participants} currency={trip.currency} />
      )}
      {tab === "checklist" && <ChecklistPanel tripId={trip.id} myRole={myRole} />}
      {tab === "itinerary" && <ItineraryPanel tripId={trip.id} myRole={myRole} />}
      {tab === "participants" && <ParticipantsPanel tripId={trip.id} myRole={myRole} />}
    </div>
  );
}
