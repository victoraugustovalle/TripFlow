import { MapContainer, Marker, Popup, TileLayer } from "react-leaflet";
import { OSM_ATTRIBUTION, OSM_TILE_URL } from "./leafletIcons";

export function MapView({
  latitude,
  longitude,
  label,
  className = "h-64 w-full",
  interactive = false,
}: {
  latitude: number;
  longitude: number;
  label?: string;
  className?: string;
  interactive?: boolean;
}) {
  return (
    <MapContainer
      key={`${latitude},${longitude}`}
      center={[latitude, longitude]}
      zoom={15}
      scrollWheelZoom={interactive}
      className={`${className} rounded-lg`}
    >
      <TileLayer attribution={OSM_ATTRIBUTION} url={OSM_TILE_URL} />
      <Marker position={[latitude, longitude]}>{label && <Popup>{label}</Popup>}</Marker>
    </MapContainer>
  );
}
