export interface ImageSearchResult {
  id: string;
  title: string;
  url: string;
  thumbnailUrl: string;
  creator: string | null;
  license: string;
}

interface OpenverseResult {
  id: string;
  title?: string;
  url: string;
  thumbnail?: string;
  creator?: string;
  license?: string;
}

interface OpenverseResponse {
  results?: OpenverseResult[];
}

/**
 * Openverse (openverse.org) indexa imagens com licenca aberta (Creative Commons) - API publica,
 * sem chave, com CORS liberado. Mesma familia de escolha que o geosearch da Wikipedia usado em
 * wikiPhoto.ts: gratis, sem cadastro, direto do navegador.
 */
export async function searchImages(query: string): Promise<ImageSearchResult[]> {
  const url = `https://api.openverse.org/v1/images/?q=${encodeURIComponent(query)}&page_size=21&mature=false`;
  const response = await fetch(url);
  if (!response.ok) throw new Error("Falha ao buscar imagens.");

  const data = (await response.json()) as OpenverseResponse;
  return (data.results ?? [])
    .filter((r) => Boolean(r.url))
    .map((r) => ({
      id: r.id,
      title: r.title ?? "",
      url: r.url,
      thumbnailUrl: r.thumbnail ?? r.url,
      creator: r.creator ?? null,
      license: (r.license ?? "").toUpperCase(),
    }));
}
