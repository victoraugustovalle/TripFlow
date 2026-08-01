import { useState } from "react";
import { Badge } from "../components/Badge";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { SkeletonLines } from "../components/Skeleton";
import { activityTypeLabels, activityTypeTone, formatDateTime } from "../utils/labels";
import { useTripActivity } from "./hooks";

const COMPACT_SIZE = 8;
const EXPANDED_SIZE = 30;

export function ActivityTimeline({ tripId }: { tripId: string }) {
  const [expanded, setExpanded] = useState(false);
  const { data, isLoading } = useTripActivity(tripId, expanded ? EXPANDED_SIZE : COMPACT_SIZE);

  return (
    <Card>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="font-display text-lg font-medium text-navy-900">Atividade recente</h2>
      </div>

      {isLoading ? (
        <SkeletonLines />
      ) : !data || data.items.length === 0 ? (
        <p className="flex items-center gap-2 text-sm text-navy-700/70">
          <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
          Nada aconteceu por aqui ainda.
        </p>
      ) : (
        <>
          <ul className="flex flex-col divide-y divide-cream-200">
            {data.items.map((entry) => (
              <li key={entry.id} className="flex items-start justify-between gap-3 py-2.5">
                <div className="flex items-start gap-2.5">
                  <Badge tone={activityTypeTone[entry.type]} className="mt-0.5 shrink-0">
                    {activityTypeLabels[entry.type]}
                  </Badge>
                  <p className="text-sm text-navy-900">{entry.message}</p>
                </div>
                <span className="shrink-0 text-xs text-navy-700/50">{formatDateTime(entry.createdAt)}</span>
              </li>
            ))}
          </ul>
          {!expanded && data.totalCount > COMPACT_SIZE && (
            <button
              type="button"
              onClick={() => setExpanded(true)}
              className="mt-3 text-xs font-medium text-brand-700 hover:underline"
            >
              Ver tudo
            </button>
          )}
        </>
      )}
    </Card>
  );
}
