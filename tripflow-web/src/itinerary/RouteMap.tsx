import L from "leaflet";
import { useEffect, useState } from "react";
import { MapContainer, Marker, Polyline, Popup, TileLayer, useMap } from "react-leaflet";
import { OSM_ATTRIBUTION, OSM_TILE_URL } from "../components/leafletIcons";
import type { RouteSegment } from "./routing";
import { fetchNearbyPhoto } from "./wikiPhoto";
import type { WikiPhoto } from "./wikiPhoto";

export interface RoutePoint {
  id: string;
  latitude: number;
  longitude: number;
  label: string;
  sublabel?: string;
}

function escapeHtml(value: string) {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

/** O ponto de destaque no canto do marcador avisa que ha uma foto do local pra ver no popup -
 * sem isso o recurso e invisivel ate a pessoa clicar por acaso. */
function numberedIcon(n: number, label: string) {
  return L.divIcon({
    className: "",
    html: `<div class="itinerary-marker" role="img" aria-label="Parada ${n}: ${escapeHtml(label)}">${n}<span class="itinerary-marker-photo-hint" aria-hidden="true"></span></div>`,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
    popupAnchor: [0, -14],
  });
}

function RouteMarker({ point, index }: { point: RoutePoint; index: number }) {
  const [photoStatus, setPhotoStatus] = useState<"idle" | "loading" | "done">("idle");
  const [photo, setPhoto] = useState<WikiPhoto | null>(null);

  // So busca a foto quando a pessoa abre o popup (clica no numero), nao pra todo marcador do mapa de uma vez.
  const loadPhoto = () => {
    if (photoStatus !== "idle") return;
    setPhotoStatus("loading");
    fetchNearbyPhoto(point.latitude, point.longitude).then((result) => {
      setPhoto(result);
      setPhotoStatus("done");
    });
  };

  return (
    <Marker
      position={[point.latitude, point.longitude]}
      icon={numberedIcon(index + 1, point.label)}
      eventHandlers={{ popupopen: loadPhoto }}
    >
      <Popup minWidth={220}>
        <strong>
          {index + 1}. {point.label}
        </strong>
        {point.sublabel && <div>{point.sublabel}</div>}
        {photoStatus === "loading" && <div className="mt-2 text-xs text-navy-700/50">Buscando foto do local...</div>}
        {photoStatus === "done" && photo && (
          <img src={photo.url} alt={photo.title} className="mt-2 h-28 w-full rounded object-cover" />
        )}
      </Popup>
    </Marker>
  );
}

function FitToPoints({ points }: { points: RoutePoint[] }) {
  const map = useMap();
  const key = points.map((p) => `${p.latitude},${p.longitude}`).join("|");

  useEffect(() => {
    if (points.length === 0) return;
    if (points.length === 1) {
      map.setView([points[0].latitude, points[0].longitude], 14);
      return;
    }
    const bounds = L.latLngBounds(points.map((p) => [p.latitude, p.longitude] as [number, number]));
    map.fitBounds(bounds, { padding: [48, 48] });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [map, key]);

  return null;
}

export function RouteMap({
  points,
  segments,
  className = "h-[70vh] w-full",
}: {
  points: RoutePoint[];
  segments: RouteSegment[];
  className?: string;
}) {
  if (points.length === 0) return null;

  return (
    <MapContainer center={[points[0].latitude, points[0].longitude]} zoom={13} scrollWheelZoom className={`${className} rounded-lg`}>
      <TileLayer attribution={OSM_ATTRIBUTION} url={OSM_TILE_URL} />
      {segments.map((segment, index) => (
        <Polyline
          key={index}
          positions={segment.path}
          pathOptions={{
            color: segment.isRoad ? "#0e9488" : "#1b2340",
            weight: 4,
            opacity: segment.isRoad ? 0.85 : 0.5,
            dashArray: segment.isRoad ? undefined : "10 8",
          }}
        />
      ))}
      {points.map((point, index) => (
        <RouteMarker key={point.id} point={point} index={index} />
      ))}
      <FitToPoints points={points} />
    </MapContainer>
  );
}
