import { useEffect, useState } from "react";
import { Modal } from "../components/Modal";
import type { ItineraryItemDto } from "../api/types";
import { formatDate, formatTime, itineraryItemTypeLabels } from "../utils/labels";
import { RouteMap } from "./RouteMap";
import type { RoutePoint } from "./RouteMap";
import { buildRouteSegments } from "./routing";
import type { RouteSegment } from "./routing";

export function DayMapModal({ date, items, onClose }: { date: string; items: ItineraryItemDto[]; onClose: () => void }) {
  const points: RoutePoint[] = items
    .filter((item): item is ItineraryItemDto & { latitude: number; longitude: number } => item.latitude != null && item.longitude != null)
    .map((item) => ({
      id: item.id,
      latitude: item.latitude,
      longitude: item.longitude,
      label: item.title,
      sublabel: [itineraryItemTypeLabels[item.type], formatTime(item.startTime)].filter(Boolean).join(" · "),
    }));

  const routeKey = points.map((p) => `${p.id}:${p.latitude}:${p.longitude}`).join("|");
  const [segments, setSegments] = useState<RouteSegment[]>([]);
  const [loadingRoute, setLoadingRoute] = useState(false);

  useEffect(() => {
    if (points.length < 2) {
      setSegments([]);
      return;
    }
    let cancelled = false;
    setLoadingRoute(true);
    buildRouteSegments(points.map((p) => [p.latitude, p.longitude]))
      .then((result) => {
        if (!cancelled) setSegments(result);
      })
      .finally(() => {
        if (!cancelled) setLoadingRoute(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [routeKey]);

  const missingCount = items.length - points.length;
  const hasRoadSegment = segments.some((s) => s.isRoad);
  const hasFallbackSegment = segments.some((s) => !s.isRoad);

  return (
    <Modal title={`Mapa do dia - ${formatDate(date)}`} onClose={onClose} size="xl">
      {points.length === 0 && <p className="text-sm text-navy-700/70">Nenhum item desse dia tem localizacao definida ainda.</p>}

      {points.length > 0 && <RouteMap points={points} segments={segments} className="h-[75vh] w-full" />}

      <div className="mt-3 flex flex-col gap-1 text-xs text-navy-700/50">
        {loadingRoute && <p>Calculando rotas entre os pontos...</p>}
        {!loadingRoute && (hasRoadSegment || hasFallbackSegment) && (
          <p className="flex flex-wrap items-center gap-x-4 gap-y-1">
            {hasRoadSegment && (
              <span className="flex items-center gap-1.5">
                <span className="inline-block h-0.5 w-5 bg-brand-600" /> rota por estrada
              </span>
            )}
            {hasFallbackSegment && (
              <span className="flex items-center gap-1.5">
                <span className="inline-block h-0.5 w-5 border-t-2 border-dashed border-navy-700" /> sem rota terrestre encontrada (trecho
                maritimo/outro)
              </span>
            )}
          </p>
        )}
        {missingCount > 0 && (
          <p>
            {missingCount} item{missingCount > 1 ? "s" : ""} desse dia sem localizacao definida - nao aparece{missingCount > 1 ? "m" : ""} no mapa.
          </p>
        )}
      </div>
    </Modal>
  );
}
