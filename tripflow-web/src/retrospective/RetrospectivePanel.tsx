import { useState } from "react";
import type { ParticipantDto } from "../api/types";
import { useAuthStore } from "../auth/authStore";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { CoverPickerModal } from "../components/CoverPickerModal";
import { FlightTrail } from "../components/FlightTrail";
import { Mascot } from "../components/Mascot";
import { SkeletonLines } from "../components/Skeleton";
import { formatCurrency, formatDate } from "../utils/labels";
import { useTripRetrospective, useUpsertTripMemory } from "./hooks";

function StarPicker({ value, onChange, readOnly, size = "text-lg" }: { value: number | null; onChange?: (v: number) => void; readOnly?: boolean; size?: string }) {
  return (
    <div className="flex gap-0.5" role={readOnly ? undefined : "radiogroup"} aria-label="Nota da viagem">
      {[1, 2, 3, 4, 5].map((star) => (
        <button
          key={star}
          type="button"
          disabled={readOnly}
          onClick={() => onChange?.(star)}
          aria-label={`${star} estrela${star > 1 ? "s" : ""}`}
          className={`${size} leading-none ${readOnly ? "cursor-default" : "cursor-pointer hover:scale-110"} transition-transform ${
            value != null && star <= value ? "text-amber-500" : "text-cream-300"
          }`}
        >
          ★
        </button>
      ))}
    </div>
  );
}

export function RetrospectivePanel({
  tripId,
  currency,
  participants,
}: {
  tripId: string;
  currency: string;
  participants: ParticipantDto[];
}) {
  const { data: retro, isLoading } = useTripRetrospective(tripId);
  const upsertMemory = useUpsertTripMemory(tripId);
  const currentUser = useAuthStore((s) => s.user);
  const myParticipantId = participants.find((p) => p.userId === currentUser?.id)?.id;

  const [isEditingMemory, setIsEditingMemory] = useState(false);
  const [highlightDraft, setHighlightDraft] = useState("");
  const [ratingDraft, setRatingDraft] = useState<number | null>(null);
  const [photoDraft, setPhotoDraft] = useState<string | null>(null);
  const [isPhotoModalOpen, setIsPhotoModalOpen] = useState(false);

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card>
          <SkeletonLines />
        </Card>
        <Card>
          <SkeletonLines />
        </Card>
      </div>
    );
  }

  if (!retro) return null;

  const dateRange = [formatDate(retro.startDate), formatDate(retro.endDate)].filter((d) => d !== "-").join(" – ");
  const checklistPct =
    retro.checklistTotalCount === 0 ? 0 : Math.round((retro.checklistDoneCount / retro.checklistTotalCount) * 100);
  const overBudget = retro.totalPlanned > 0 && retro.totalSpent > retro.totalPlanned;
  const myMemory = retro.memories.find((m) => m.participantId === myParticipantId);

  const startEditMemory = () => {
    setHighlightDraft(myMemory?.highlight ?? "");
    setRatingDraft(myMemory?.rating ?? null);
    setPhotoDraft(myMemory?.photoUrl ?? null);
    setIsEditingMemory(true);
  };

  const handleSaveMemory = async () => {
    await upsertMemory.mutateAsync({
      highlight: highlightDraft.trim() || null,
      rating: ratingDraft,
      photoUrl: photoDraft,
    });
    setIsEditingMemory(false);
  };

  return (
    <div className="flex flex-col gap-6">
      <Card className="relative overflow-hidden">
        <FlightTrail className="pointer-events-none absolute -right-6 -top-6 h-24 w-48 text-brand-600/10" />
        <div className="relative flex flex-wrap items-center justify-between gap-4">
          <div>
            <h2 className="font-display text-xl font-semibold text-navy-900">{retro.tripName}</h2>
            <p className="mt-1 text-sm text-navy-700/70">
              {dateRange || "Sem datas definidas"}
              {retro.durationDays != null && ` · ${retro.durationDays} ${retro.durationDays === 1 ? "dia" : "dias"}`}
              {" · "}
              {retro.participantsCount} {retro.participantsCount === 1 ? "participante" : "participantes"}
            </p>
            {retro.averageRating != null && (
              <div className="mt-3 flex items-center gap-2">
                <StarPicker value={Math.round(retro.averageRating)} readOnly />
                <span className="text-sm text-navy-700/70">{retro.averageRating.toFixed(1)} de 5 (media do grupo)</span>
              </div>
            )}
          </div>
          <Mascot pose="celebrating" className="h-24 w-24 shrink-0 sm:h-28 sm:w-28" />
        </div>
      </Card>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card>
          <h3 className="font-display mb-4 text-lg font-medium text-navy-900">Total gasto</h3>
          <p className={`text-3xl font-semibold ${overBudget ? "text-coral-700" : "text-navy-900"}`}>
            {formatCurrency(retro.totalSpent, currency)}
          </p>
          {retro.totalPlanned > 0 && (
            <p className="mt-1 text-sm text-navy-700/70">
              de {formatCurrency(retro.totalPlanned, currency)} planejados
              {overBudget && " — estourou o orcamento"}
            </p>
          )}
        </Card>

        <Card>
          <h3 className="font-display mb-4 text-lg font-medium text-navy-900">Quem gastou mais</h3>
          {retro.topSpenders.length === 0 ? (
            <p className="flex items-center gap-2 text-sm text-navy-700/70">
              <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
              Nenhum gasto lancado nessa viagem.
            </p>
          ) : (
            <ul className="flex flex-col gap-2">
              {retro.topSpenders.map((spender, index) => (
                <li key={spender.participantId} className="flex items-center justify-between gap-3 text-sm">
                  <span className="flex items-center gap-2">
                    <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-brand-600 text-xs font-semibold text-white">
                      {index + 1}
                    </span>
                    <span className="text-navy-900">{spender.displayName}</span>
                  </span>
                  <span className="font-medium text-navy-900">{formatCurrency(spender.totalPaid, currency)}</span>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card>
          <h3 className="font-display mb-4 text-lg font-medium text-navy-900">Checklist</h3>
          {retro.checklistTotalCount === 0 ? (
            <p className="text-sm text-navy-700/70">Nenhum item de checklist nessa viagem.</p>
          ) : (
            <div className="flex flex-col gap-2">
              <div className="flex items-baseline justify-between text-sm">
                <span className="text-navy-700/70">
                  {retro.checklistDoneCount} de {retro.checklistTotalCount} concluido
                </span>
                <span className="font-semibold text-navy-900">{checklistPct}%</span>
              </div>
              <div className="h-1.5 w-full overflow-hidden rounded-full bg-cream-200">
                <div className="h-full rounded-full bg-brand-600" style={{ width: `${checklistPct}%` }} />
              </div>
            </div>
          )}
        </Card>

        <Card>
          <h3 className="font-display mb-4 text-lg font-medium text-navy-900">Roteiro</h3>
          <p className="text-3xl font-semibold text-navy-900">{retro.itineraryItemsCount}</p>
          <p className="mt-1 text-sm text-navy-700/70">
            {retro.itineraryItemsCount === 1 ? "item planejado" : "itens planejados"}
          </p>
        </Card>
      </div>

      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
          <h3 className="font-display text-lg font-medium text-navy-900">Memorias da viagem</h3>
          {myParticipantId && !isEditingMemory && (
            <Button variant="secondary" onClick={startEditMemory}>
              {myMemory ? "Editar minha memoria" : "Adicionar minha memoria"}
            </Button>
          )}
        </div>

        {isEditingMemory && (
          <div className="mb-4 flex flex-col gap-3 rounded-lg border border-cream-300 p-3">
            <div className="flex items-center gap-3">
              {photoDraft ? (
                <img src={photoDraft} alt="" className="h-16 w-16 shrink-0 rounded-lg object-cover" />
              ) : (
                <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-lg bg-cream-200 text-xs text-navy-700/50">
                  Sem foto
                </div>
              )}
              <Button type="button" variant="ghost" onClick={() => setIsPhotoModalOpen(true)}>
                {photoDraft ? "Trocar foto" : "Escolher foto"}
              </Button>
            </div>

            <div>
              <span className="mb-1 block text-sm font-medium text-navy-900">Nota da viagem</span>
              <StarPicker value={ratingDraft} onChange={setRatingDraft} size="text-2xl" />
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-navy-900" htmlFor="memory-highlight">
                Melhor momento (opcional)
              </label>
              <textarea
                id="memory-highlight"
                value={highlightDraft}
                onChange={(e) => setHighlightDraft(e.target.value)}
                rows={3}
                maxLength={500}
                className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                  transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
              />
            </div>

            <div className="flex gap-3">
              <Button type="button" isLoading={upsertMemory.isPending} onClick={handleSaveMemory}>
                Salvar
              </Button>
              <Button type="button" variant="secondary" onClick={() => setIsEditingMemory(false)}>
                Cancelar
              </Button>
            </div>
          </div>
        )}

        {isPhotoModalOpen && (
          <CoverPickerModal
            onSelect={(url) => {
              setPhotoDraft(url);
              setIsPhotoModalOpen(false);
            }}
            onClose={() => setIsPhotoModalOpen(false)}
          />
        )}

        {retro.memories.length === 0 ? (
          <p className="flex items-center gap-2 text-sm text-navy-700/70">
            <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
            Ninguem registrou uma memoria dessa viagem ainda.
          </p>
        ) : (
          // Cartoes estilo polaroid (rotacao alternada + "fita" no topo) pra essa ser a tela
          // com mais peso emocional do produto, nao so mais uma lista de itens.
          <div className="grid grid-cols-1 gap-6 py-2 sm:grid-cols-2 lg:grid-cols-3">
            {retro.memories.map((m, index) => (
              <div
                key={m.participantId}
                className={`relative rounded-sm bg-white p-3 pb-4 shadow-md ${index % 2 === 0 ? "-rotate-1" : "rotate-1"}`}
              >
                <span
                  aria-hidden="true"
                  className={`absolute -top-2.5 left-1/2 h-5 w-16 -translate-x-1/2 rounded-sm bg-coral-200/70 shadow-sm ${
                    index % 2 === 0 ? "-rotate-3" : "rotate-2"
                  }`}
                />
                {m.photoUrl ? (
                  <img src={m.photoUrl} alt="" className="aspect-square w-full rounded-sm object-cover" />
                ) : (
                  <div className="flex aspect-square w-full items-center justify-center rounded-sm bg-cream-100">
                    <Mascot pose="wave" className="h-16 w-16 opacity-50" />
                  </div>
                )}
                <div className="mt-3 flex flex-col items-center gap-1 text-center">
                  <span className="font-display text-sm font-semibold text-navy-900">{m.displayName}</span>
                  {m.rating != null && <StarPicker value={m.rating} readOnly size="text-xs" />}
                  {m.highlight && <p className="text-xs text-navy-700/70">{m.highlight}</p>}
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}
