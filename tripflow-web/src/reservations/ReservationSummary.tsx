import type { ReservationDto } from "../api/types";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { formatCurrency, formatDateTime, reservationTypeLabels } from "../utils/labels";

export function ReservationSummary({
  reservation,
  currency,
  canEdit,
  onEdit,
  onDelete,
  isDeleting,
  onLaunchExpense,
}: {
  reservation: ReservationDto;
  currency: string;
  canEdit: boolean;
  onEdit: (reservation: ReservationDto) => void;
  onDelete: (reservationId: string) => void;
  isDeleting: boolean;
  /** So aparece quando ha um preco cadastrado - sem valor, "ja preenchido" vira so metade da promessa. */
  onLaunchExpense?: (reservation: ReservationDto) => void;
}) {
  const metaParts = [
    reservation.providerName,
    reservation.confirmationCode ? `cod. ${reservation.confirmationCode}` : null,
    formatDateTime(reservation.startAt),
    reservation.location,
    reservation.price != null ? formatCurrency(reservation.price, reservation.currency ?? currency) : null,
  ].filter(Boolean);

  return (
    <div className="flex items-start justify-between gap-3 rounded-lg border border-cream-300 bg-cream-100/60 px-3 py-2">
      <div>
        <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
          <Badge tone="neutral">{reservationTypeLabels[reservation.type]}</Badge>
          <span className="text-sm font-medium text-navy-900">{reservation.title}</span>
        </div>
        {metaParts.length > 0 && <p className="mt-1 text-xs text-navy-700/50">{metaParts.join(" · ")}</p>}
        {reservation.notes && <p className="mt-1 text-xs text-navy-700/70">{reservation.notes}</p>}
      </div>
      {canEdit && (
        <div className="flex shrink-0 gap-1">
          {onLaunchExpense && reservation.price != null && (
            <Button variant="ghost" onClick={() => onLaunchExpense(reservation)}>
              Lancar como gasto
            </Button>
          )}
          <Button variant="ghost" onClick={() => onEdit(reservation)}>
            Editar
          </Button>
          <Button variant="ghostDanger" onClick={() => onDelete(reservation.id)} disabled={isDeleting}>
            Remover
          </Button>
        </div>
      )}
    </div>
  );
}
