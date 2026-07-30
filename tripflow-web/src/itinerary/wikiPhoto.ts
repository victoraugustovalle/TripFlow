export interface WikiPhoto {
  url: string;
  title: string;
}

interface GeoSearchResponse {
  query?: { geosearch?: { pageid: number; title: string }[] };
}

interface PageImagesResponse {
  query?: { pages?: Record<string, { thumbnail?: { source: string } }> };
}

/**
 * Busca uma foto ilustrativa do local via geosearch da Wikipedia (API publica, sem chave,
 * suporta CORS com origin=*) - acha o artigo mais proximo das coordenadas e pega a imagem
 * de capa dele. Tenta em portugues primeiro, cai pro ingles se nao achar nada por perto.
 */
async function geoSearchPhoto(lang: string, latitude: number, longitude: number): Promise<WikiPhoto | null> {
  try {
    const searchUrl = `https://${lang}.wikipedia.org/w/api.php?action=query&list=geosearch&gscoord=${latitude}|${longitude}&gsradius=2000&gslimit=1&format=json&origin=*`;
    const searchResponse = await fetch(searchUrl);
    if (!searchResponse.ok) return null;

    const searchData = (await searchResponse.json()) as GeoSearchResponse;
    const page = searchData.query?.geosearch?.[0];
    if (!page) return null;

    const imageUrl = `https://${lang}.wikipedia.org/w/api.php?action=query&pageids=${page.pageid}&prop=pageimages&piprop=thumbnail&pithumbsize=500&format=json&origin=*`;
    const imageResponse = await fetch(imageUrl);
    if (!imageResponse.ok) return null;

    const imageData = (await imageResponse.json()) as PageImagesResponse;
    const thumbnail = imageData.query?.pages?.[String(page.pageid)]?.thumbnail?.source;
    if (!thumbnail) return null;

    return { url: thumbnail, title: page.title };
  } catch {
    return null;
  }
}

export async function fetchNearbyPhoto(latitude: number, longitude: number): Promise<WikiPhoto | null> {
  return (await geoSearchPhoto("pt", latitude, longitude)) ?? (await geoSearchPhoto("en", latitude, longitude));
}
