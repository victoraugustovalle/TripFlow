import type { ItineraryItemDto, ReservationDto } from "../api/types";
import { Button } from "../components/Button";
import { Modal } from "../components/Modal";
import { formatCurrency, formatDate, formatTime, itineraryItemTypeLabels, reservationTypeLabels } from "../utils/labels";

/** Visao compacta e imprimivel do dia - pensada pra usar (ou levar impressa) com internet ruim
 * durante a viagem, sem depender do mapa carregar tiles. */
export function DaySummaryModal({
  date,
  items,
  reservationsByItemId,
  currency,
  onClose,
}: {
  date: string;
  items: ItineraryItemDto[];
  reservationsByItemId: Map<string, ReservationDto[]>;
  currency: string;
  onClose: () => void;
}) {
  return (
    <Modal title={`Resumo do dia - ${formatDate(date)}`} onClose={onClose}>
      <div className="print-day-summary">
        <div className="mb-3 flex justify-end print:hidden">
          <Button type="button" variant="secondary" onClick={() => window.print()}>
            Imprimir
          </Button>
        </div>

        <h2 className="mb-3 hidden font-display text-xl font-semibold text-navy-900 print:block">{formatDate(date)}</h2>

        {items.length === 0 ? (
          <p className="text-sm text-navy-700/70">Nenhum item nesse dia.</p>
        ) : (
          <ol className="flex flex-col divide-y divide-cream-200">
            {items.map((item) => {
              const reservations = reservationsByItemId.get(item.id) ?? [];
              return (
                <li key={item.id} className="py-3 first:pt-0">
                  <div className="flex flex-wrap items-baseline gap-x-2">
                    {formatTime(item.startTime) && (
                      <span className="text-sm font-semibold text-navy-900">{formatTime(item.startTime)}</span>
                    )}
                    <span className="text-sm font-medium text-navy-900">{item.title}</span>
                    <span className="text-xs text-navy-700/50">({itineraryItemTypeLabels[item.type]})</span>
                  </div>
                  {item.location && <p className="mt-0.5 text-xs text-navy-700/60">{item.location}</p>}
                  {item.description && <p className="mt-1 text-sm text-navy-700/70">{item.description}</p>}

                  {reservations.length > 0 && (
                    <ul className="mt-2 flex flex-col gap-1 border-l-2 border-cream-300 pl-3">
                      {reservations.map((r) => (
                        <li key={r.id} className="text-xs text-navy-700/70">
                          <strong className="text-navy-900">{reservationTypeLabels[r.type]}:</strong> {r.title}
                          {r.providerName && ` · ${r.providerName}`}
                          {r.confirmationCode && ` · cod. ${r.confirmationCode}`}
                          {r.price != null && ` · ${formatCurrency(r.price, r.currency ?? currency)}`}
                        </li>
                      ))}
                    </ul>
                  )}
                </li>
              );
            })}
          </ol>
        )}
      </div>
    </Modal>
  );
}
