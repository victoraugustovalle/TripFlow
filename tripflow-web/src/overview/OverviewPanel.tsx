import type { ParticipantDto, SettlementTransferDto } from "../api/types";
import { ActivityTimeline } from "../activity/ActivityTimeline";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { Mascot } from "../components/Mascot";
import { SkeletonLines } from "../components/Skeleton";
import { useAuthStore } from "../auth/authStore";
import { useConfirmSettlement, useMarkSettlementPaid } from "../expenses/hooks";
import { formatCurrency, formatDate, formatTime, itineraryItemTypeLabels } from "../utils/labels";
import { useTripOverview } from "./hooks";

type TripTab = "expenses" | "checklist" | "itinerary" | "participants" | "documents";

/** >=100% = tudo resolvido (verde); acima da metade = a caminho (ambar); senao, ainda cru (coral). */
function readinessBarTone(percent: number) {
  if (percent >= 100) return "bg-brand-600";
  if (percent >= 50) return "bg-amber-500";
  return "bg-coral-600";
}

function participantName(participants: ParticipantDto[], participantId: string) {
  const participant = participants.find((p) => p.id === participantId);
  return participant?.displayName ?? participant?.invitedEmail ?? "Participante removido";
}

/** Sem orcamento definido pra categoria, so mostra que ha gasto (barra neutra cheia); com
 * orcamento, verde dentro do limite e coral quando estourou. */
function budgetBarTone(actual: number, planned: number) {
  if (planned <= 0) return "bg-navy-700/30";
  return actual > planned ? "bg-coral-600" : "bg-brand-600";
}

export function OverviewPanel({
  tripId,
  currency,
  participants,
  onNavigate,
}: {
  tripId: string;
  currency: string;
  participants: ParticipantDto[];
  onNavigate: (tab: TripTab) => void;
}) {
  const { data: overview, isLoading } = useTripOverview(tripId);
  const markSettlementPaid = useMarkSettlementPaid(tripId);
  const confirmSettlement = useConfirmSettlement(tripId);
  const currentUser = useAuthStore((s) => s.user);
  const myParticipantId = participants.find((p) => p.userId === currentUser?.id)?.id;

  const findPendingSettlement = (transfer: SettlementTransferDto) =>
    overview?.settlement.myPendingSettlements.find(
      (r) => r.fromParticipantId === transfer.fromParticipantId && r.toParticipantId === transfer.toParticipantId,
    );

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

  if (!overview) return null;

  const { budget, checklist, settlement, upcomingItineraryItems, pendingInvitesCount, readiness } = overview;
  const checklistPct = checklist.totalCount === 0 ? 0 : Math.round((checklist.doneCount / checklist.totalCount) * 100);

  return (
    <div className="flex flex-col gap-6">
      {pendingInvitesCount > 0 && (
        <button
          type="button"
          onClick={() => onNavigate("participants")}
          className="flex items-center justify-between rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-left text-sm text-navy-900 transition-colors hover:bg-amber-100"
        >
          <span>
            <strong>{pendingInvitesCount}</strong>{" "}
            {pendingInvitesCount === 1 ? "convite pendente" : "convites pendentes"} pra essa viagem.
          </span>
          <span className="text-xs font-semibold text-brand-700">Ver participantes</span>
        </button>
      )}

      <Card>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-display text-lg font-medium text-navy-900">Prontidao da viagem</h2>
          <div className="flex items-center gap-2">
            {readiness.percentReady >= 100 && <Mascot pose="celebrating" className="readiness-bounce h-8 w-8" />}
            <span className="text-sm font-semibold text-navy-900">{readiness.percentReady}%</span>
          </div>
        </div>

        <div className="h-1.5 w-full overflow-hidden rounded-full bg-cream-200">
          <div
            className={`h-full rounded-full transition-all ${readinessBarTone(readiness.percentReady)}`}
            style={{ width: `${readiness.percentReady}%` }}
          />
        </div>

        {readiness.pendingItems.length === 0 ? (
          <p className="mt-4 text-sm text-navy-700/70">Tudo pronto - nenhuma pendencia encontrada pra essa viagem.</p>
        ) : (
          <ul className="mt-3 flex flex-col divide-y divide-cream-200">
            {readiness.pendingItems.map((item) => (
              <li key={item.key}>
                <button
                  type="button"
                  onClick={() => onNavigate(item.targetTab as TripTab)}
                  className="flex w-full items-center justify-between gap-3 py-2.5 text-left text-sm text-navy-900 transition-colors hover:text-brand-700"
                >
                  <span>{item.label}</span>
                  <span className="shrink-0 text-xs font-semibold text-brand-700">Resolver</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </Card>

      <ActivityTimeline tripId={tripId} />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card>
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-display text-lg font-medium text-navy-900">Orcamento</h2>
            <button
              type="button"
              onClick={() => onNavigate("expenses")}
              className="text-xs font-medium text-brand-700 hover:underline"
            >
              Ver gastos
            </button>
          </div>

          {budget.categories.length === 0 ? (
            <p className="flex items-center gap-2 text-sm text-navy-700/70">
              <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
              Nenhum gasto ou orcamento cadastrado ainda.
            </p>
          ) : (
            <div className="flex flex-col gap-4">
              <div className="flex items-baseline justify-between">
                <span className="text-sm text-navy-700/70">Total</span>
                <span className="text-sm font-semibold text-navy-900">
                  {formatCurrency(budget.totalActual, currency)}
                  {budget.totalPlanned > 0 && (
                    <span className="font-normal text-navy-700/50"> de {formatCurrency(budget.totalPlanned, currency)}</span>
                  )}
                </span>
              </div>
              <ul className="flex flex-col gap-3">
                {budget.categories.map((c) => (
                  <li key={c.category}>
                    <div className="mb-1 flex items-baseline justify-between text-xs">
                      <span className="font-medium text-navy-900">{c.category}</span>
                      <span className="text-navy-700/70">
                        {formatCurrency(c.actualAmount, currency)}
                        {c.plannedAmount > 0 && ` / ${formatCurrency(c.plannedAmount, currency)}`}
                      </span>
                    </div>
                    <div className="h-1.5 w-full overflow-hidden rounded-full bg-cream-200">
                      <div
                        className={`h-full rounded-full ${budgetBarTone(c.actualAmount, c.plannedAmount)}`}
                        style={{
                          width: `${c.plannedAmount > 0 ? Math.min(100, (c.actualAmount / c.plannedAmount) * 100) : 100}%`,
                        }}
                      />
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </Card>

        <Card>
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-display text-lg font-medium text-navy-900">Saldo</h2>
            <button
              type="button"
              onClick={() => onNavigate("expenses")}
              className="text-xs font-medium text-brand-700 hover:underline"
            >
              Ver gastos
            </button>
          </div>

          <div className="mb-4">
            {settlement.myNetAmount > 0.01 && (
              <Badge tone="success">Te devem {formatCurrency(settlement.myNetAmount, currency)}</Badge>
            )}
            {settlement.myNetAmount < -0.01 && (
              <Badge tone="danger">Voce deve {formatCurrency(-settlement.myNetAmount, currency)}</Badge>
            )}
            {Math.abs(settlement.myNetAmount) <= 0.01 && <Badge tone="neutral">Contas em dia</Badge>}
          </div>

          {settlement.myTransfers.length > 0 && (
            <ul className="flex flex-col gap-2 text-sm text-navy-700/70">
              {settlement.myTransfers.map((transfer, index) => {
                const iOwe = myParticipantId != null && transfer.fromParticipantId === myParticipantId;
                const owedToMe = myParticipantId != null && transfer.toParticipantId === myParticipantId;
                const pendingRecord = findPendingSettlement(transfer);

                return (
                  <li key={index} className="flex flex-wrap items-center justify-between gap-2">
                    <span>
                      <strong className="text-navy-900">{participantName(participants, transfer.fromParticipantId)}</strong> deve{" "}
                      {formatCurrency(transfer.amount, currency)} pra{" "}
                      <strong className="text-navy-900">{participantName(participants, transfer.toParticipantId)}</strong>
                    </span>

                    {pendingRecord ? (
                      owedToMe ? (
                        <Button
                          variant="secondary"
                          onClick={() => confirmSettlement.mutate(pendingRecord.id)}
                          isLoading={confirmSettlement.isPending}
                        >
                          Confirmar
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
          )}
        </Card>

        <Card>
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-display text-lg font-medium text-navy-900">Checklist</h2>
            <button
              type="button"
              onClick={() => onNavigate("checklist")}
              className="text-xs font-medium text-brand-700 hover:underline"
            >
              Ver checklist
            </button>
          </div>

          {checklist.totalCount === 0 ? (
            <p className="flex items-center gap-2 text-sm text-navy-700/70">
              <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
              Nada na checklist ainda.
            </p>
          ) : (
            <div className="flex flex-col gap-2">
              <div className="flex items-baseline justify-between text-sm">
                <span className="text-navy-700/70">
                  {checklist.doneCount} de {checklist.totalCount} feito
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
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-display text-lg font-medium text-navy-900">Proximo no roteiro</h2>
            <button
              type="button"
              onClick={() => onNavigate("itinerary")}
              className="text-xs font-medium text-brand-700 hover:underline"
            >
              Ver roteiro
            </button>
          </div>

          {upcomingItineraryItems.length === 0 ? (
            <p className="flex items-center gap-2 text-sm text-navy-700/70">
              <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
              Nada agendado a partir de hoje.
            </p>
          ) : (
            <ul className="flex flex-col divide-y divide-cream-200">
              {upcomingItineraryItems.map((item) => (
                <li key={item.id} className="flex items-center justify-between gap-3 py-2.5">
                  <div>
                    <p className="text-sm font-medium text-navy-900">{item.title}</p>
                    <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1">
                      <Badge tone="neutral">{itineraryItemTypeLabels[item.type]}</Badge>
                      <span className="text-xs text-navy-700/50">
                        {[formatDate(item.itemDate), formatTime(item.startTime), item.location].filter(Boolean).join(" · ")}
                      </span>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
    </div>
  );
}
