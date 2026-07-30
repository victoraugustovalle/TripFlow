export interface RouteSegment {
  path: [number, number][];
  isRoad: boolean;
}

interface OsrmResponse {
  code: string;
  routes?: { geometry: { coordinates: [number, number][] } }[];
}

/**
 * Usa o servidor publico de demonstracao do OSRM (sem chave, uso leve) pra tracar a rota
 * real por estrada entre dois pontos. Quando nao existe rota terrestre (ex: uma travessia
 * de barco entre ilhas), o OSRM devolve "NoRoute" e a gente cai pra uma linha reta entre os
 * pontos, tracejada, pra deixar visualmente claro que ali nao e uma rota rastreada de verdade.
 */
async function fetchRoadRoute(from: [number, number], to: [number, number]): Promise<[number, number][] | null> {
  try {
    const url = `https://router.project-osrm.org/route/v1/driving/${from[1]},${from[0]};${to[1]},${to[0]}?overview=full&geometries=geojson`;
    const response = await fetch(url);
    if (!response.ok) return null;

    const data = (await response.json()) as OsrmResponse;
    const coordinates = data.routes?.[0]?.geometry.coordinates;
    if (data.code !== "Ok" || !coordinates) return null;

    return coordinates.map(([lon, lat]) => [lat, lon]);
  } catch {
    return null;
  }
}

export async function buildRouteSegments(points: [number, number][]): Promise<RouteSegment[]> {
  const pairs = points.slice(0, -1).map((point, index) => [point, points[index + 1]] as const);

  return Promise.all(
    pairs.map(async ([from, to]) => {
      const road = await fetchRoadRoute(from, to);
      if (road) return { path: road, isRoad: true };
      return { path: [from, to], isRoad: false };
    }),
  );
}
