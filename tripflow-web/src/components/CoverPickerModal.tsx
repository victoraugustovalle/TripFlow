import { type ChangeEvent, useEffect, useRef, useState } from "react";
import { searchImages } from "../api/imageSearch";
import type { ImageSearchResult } from "../api/imageSearch";
import { Button } from "./Button";
import { Modal } from "./Modal";
import { Spinner } from "./Spinner";

const MAX_COVER_WIDTH = 1600;
const JPEG_QUALITY = 0.82;
const MIN_QUERY_LENGTH = 3;
const DEBOUNCE_MS = 500;

/** Reduz a foto pra no maximo 1600px de largura e reencoda como JPEG antes de virar data URL -
 * sem isso, uma foto de celular direto vira um texto base64 de dezenas de MB no banco. */
function readAndResizeImage(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(reader.error ?? new Error("Falha ao ler o arquivo."));
    reader.onload = () => {
      const img = new Image();
      img.onerror = () => reject(new Error("Nao foi possivel ler essa imagem."));
      img.onload = () => {
        const scale = Math.min(1, MAX_COVER_WIDTH / img.width);
        const canvas = document.createElement("canvas");
        canvas.width = Math.max(1, Math.round(img.width * scale));
        canvas.height = Math.max(1, Math.round(img.height * scale));
        const ctx = canvas.getContext("2d");
        if (!ctx) {
          reject(new Error("Canvas indisponivel."));
          return;
        }
        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
        resolve(canvas.toDataURL("image/jpeg", JPEG_QUALITY));
      };
      img.src = reader.result as string;
    };
    reader.readAsDataURL(file);
  });
}

type Tab = "device" | "web";

export function CoverPickerModal({ onSelect, onClose }: { onSelect: (url: string) => void; onClose: () => void }) {
  const [tab, setTab] = useState<Tab>("device");

  const [devicePreview, setDevicePreview] = useState<string | null>(null);
  const [deviceError, setDeviceError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [query, setQuery] = useState("");
  const [results, setResults] = useState<ImageSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [searchError, setSearchError] = useState(false);
  const [selectedResult, setSelectedResult] = useState<ImageSearchResult | null>(null);

  useEffect(() => {
    const trimmed = query.trim();
    if (trimmed.length < MIN_QUERY_LENGTH) {
      setResults([]);
      return;
    }
    let cancelled = false;
    setIsSearching(true);
    setSearchError(false);
    const timeout = setTimeout(() => {
      searchImages(trimmed)
        .then((data) => {
          if (!cancelled) setResults(data);
        })
        .catch(() => {
          if (!cancelled) setSearchError(true);
        })
        .finally(() => {
          if (!cancelled) setIsSearching(false);
        });
    }, DEBOUNCE_MS);
    return () => {
      cancelled = true;
      clearTimeout(timeout);
    };
  }, [query]);

  const onFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setDeviceError(null);

    if (!file.type.startsWith("image/")) {
      setDeviceError("Escolha um arquivo de imagem.");
      return;
    }

    try {
      const dataUrl = await readAndResizeImage(file);
      setDevicePreview(dataUrl);
    } catch {
      setDeviceError("Nao foi possivel processar essa imagem. Tenta outra.");
    }
  };

  return (
    <Modal title="Capa da viagem" onClose={onClose}>
      <div className="mb-4 flex w-fit gap-1 rounded-full border border-cream-300 p-1 text-sm">
        <button
          type="button"
          onClick={() => setTab("device")}
          className={`rounded-full px-3 py-1 font-medium transition-colors ${
            tab === "device" ? "bg-brand-700 text-white" : "text-navy-700/70 hover:bg-cream-100"
          }`}
        >
          Do dispositivo
        </button>
        <button
          type="button"
          onClick={() => setTab("web")}
          className={`rounded-full px-3 py-1 font-medium transition-colors ${
            tab === "web" ? "bg-brand-700 text-white" : "text-navy-700/70 hover:bg-cream-100"
          }`}
        >
          Buscar na web
        </button>
      </div>

      {tab === "device" && (
        <div className="flex flex-col gap-3">
          <input ref={fileInputRef} type="file" accept="image/*" onChange={onFileChange} className="hidden" />
          {devicePreview ? (
            <div className="overflow-hidden rounded-lg border border-cream-300">
              <img src={devicePreview} alt="Previa da capa" className="h-48 w-full object-cover" />
            </div>
          ) : (
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              className="flex h-48 flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-cream-300
                text-sm text-navy-700/70 transition-colors hover:border-brand-400 hover:text-brand-700"
            >
              <span>Clique pra escolher uma foto do seu dispositivo</span>
              <span className="text-xs text-navy-700/50">JPEG ou PNG</span>
            </button>
          )}
          {deviceError && <p className="text-sm text-coral-700">{deviceError}</p>}
          <div className="flex justify-end gap-3">
            {devicePreview && (
              <Button type="button" variant="secondary" onClick={() => fileInputRef.current?.click()}>
                Trocar foto
              </Button>
            )}
            <Button type="button" disabled={!devicePreview} onClick={() => devicePreview && onSelect(devicePreview)}>
              Usar esta foto
            </Button>
          </div>
        </div>
      )}

      {tab === "web" && (
        <div className="flex flex-col gap-3">
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Ex: Rio de Janeiro, praia, montanha..."
            autoFocus
            className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
              transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
          />

          {isSearching && <Spinner label="Buscando fotos..." />}
          {!isSearching && searchError && <p className="text-sm text-coral-700">Nao foi possivel buscar fotos agora.</p>}
          {!isSearching && !searchError && query.trim().length >= MIN_QUERY_LENGTH && results.length === 0 && (
            <p className="text-sm text-navy-700/70">Nenhuma foto encontrada.</p>
          )}

          {results.length > 0 && (
            <div className="grid max-h-72 grid-cols-3 gap-2 overflow-y-auto">
              {results.map((result) => (
                <button
                  key={result.id}
                  type="button"
                  onClick={() => setSelectedResult(result)}
                  className={`aspect-square overflow-hidden rounded-lg border-2 transition-colors ${
                    selectedResult?.id === result.id ? "border-brand-600" : "border-transparent hover:border-cream-300"
                  }`}
                >
                  <img src={result.thumbnailUrl} alt={result.title} className="h-full w-full object-cover" loading="lazy" />
                </button>
              ))}
            </div>
          )}

          {selectedResult && (
            <p className="text-xs text-navy-700/50">
              Foto de {selectedResult.creator ?? "autor desconhecido"} · {selectedResult.license}
            </p>
          )}

          <div className="flex justify-end">
            <Button type="button" disabled={!selectedResult} onClick={() => selectedResult && onSelect(selectedResult.url)}>
              Usar esta foto
            </Button>
          </div>
        </div>
      )}
    </Modal>
  );
}
