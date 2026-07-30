import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import type { GeocodeResultDto, ItineraryItemDto, ItineraryItemType, TripRole } from "../api/types";
import { Alert } from "../components/Alert";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { Input } from "../components/Input";
import { MapView } from "../components/MapView";
import { Modal } from "../components/Modal";
import { SkeletonLines } from "../components/Skeleton";
import { formatDate, formatTime, itineraryItemTypeLabels } from "../utils/labels";
import { DayMapModal } from "./DayMapModal";
import { useCreateItineraryItem, useDeleteItineraryItem, useGeocodeSearch, useItinerary, useUpdateItineraryItem } from "./hooks";

const schema = z.object({
  title: z.string().min(1, "Informe um titulo."),
  description: z.string().optional(),
  type: z.enum(["0", "1", "2", "3", "4"]),
  itemDate: z.string().min(1, "Informe a data."),
  startTime: z.string().optional(),
  endTime: z.string().optional(),
  location: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const MIN_QUERY_LENGTH = 3;
const DEBOUNCE_MS = 450;

/** O <input type="time"> manda "HH:mm" - o TimeOnly do backend espera segundos. */
function toTimeOnly(value: string | undefined) {
  return value ? `${value}:00` : null;
}

type ViewMode = "byDay" | "all";

function ItineraryItemRow({
  item,
  canEdit,
  showDate,
  onEdit,
  onDelete,
  onShowMap,
  isDeleting,
}: {
  item: ItineraryItemDto;
  canEdit: boolean;
  showDate: boolean;
  onEdit: (item: ItineraryItemDto) => void;
  onDelete: (itemId: string) => void;
  onShowMap: (item: ItineraryItemDto) => void;
  isDeleting: boolean;
}) {
  const metaParts = [
    showDate ? formatDate(item.itemDate) : null,
    formatTime(item.startTime)
      ? `${formatTime(item.startTime)}${formatTime(item.endTime) ? `-${formatTime(item.endTime)}` : ""}`
      : null,
    item.location,
  ].filter(Boolean);

  return (
    <li className="flex flex-col gap-2 py-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium text-navy-900">{item.title}</p>
          <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1">
            <Badge tone="neutral">{itineraryItemTypeLabels[item.type]}</Badge>
            {metaParts.length > 0 && <span className="text-xs text-navy-700/50">{metaParts.join(" · ")}</span>}
          </div>
          {item.description && <p className="mt-1 text-sm text-navy-700/70">{item.description}</p>}
          {item.latitude != null && item.longitude != null && (
            <button type="button" onClick={() => onShowMap(item)} className="mt-1 text-xs font-medium text-brand-700 hover:underline">
              Ver no mapa
            </button>
          )}
        </div>
        {canEdit && (
          <div className="flex shrink-0 gap-1">
            <Button variant="ghost" onClick={() => onEdit(item)}>
              Editar
            </Button>
            <Button variant="ghostDanger" onClick={() => onDelete(item.id)} disabled={isDeleting}>
              Remover
            </Button>
          </div>
        )}
      </div>
    </li>
  );
}

export function ItineraryPanel({ tripId, myRole }: { tripId: string; myRole: TripRole | undefined }) {
  const { data: items, isLoading } = useItinerary(tripId);
  const createItem = useCreateItineraryItem(tripId);
  const updateItem = useUpdateItineraryItem(tripId);
  const deleteItem = useDeleteItineraryItem(tripId);
  const geocodeSearch = useGeocodeSearch();

  const [results, setResults] = useState<GeocodeResultDto[]>([]);
  const [selectedCoords, setSelectedCoords] = useState<{ latitude: number; longitude: number } | null>(null);
  const [mapModalItem, setMapModalItem] = useState<ItineraryItemDto | null>(null);
  const [dayMapDate, setDayMapDate] = useState<string | null>(null);
  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<ViewMode>("byDay");
  const lastPickedRef = useRef<string | null>(null);
  const searchIdRef = useRef(0);

  const canEdit = myRole === 1 || myRole === 2;

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { type: "0", itemDate: new Date().toISOString().slice(0, 10) },
  });

  const locationValue = watch("location");

  const runSearch = (query: string) => {
    const searchId = ++searchIdRef.current;
    setSelectedCoords(null);
    geocodeSearch.mutate(query, {
      onSuccess: (data) => {
        if (searchIdRef.current === searchId) setResults(data);
      },
    });
  };

  // Autocompleta enquanto digita, mas com debounce: o geocoding tem limite global de 1 req/s
  // (compartilhado por toda a API), entao buscar a cada tecla estouraria isso na hora.
  useEffect(() => {
    const query = (locationValue ?? "").trim();
    if (!query || query.length < MIN_QUERY_LENGTH || query === lastPickedRef.current) {
      setResults([]);
      return;
    }
    const timeout = setTimeout(() => runSearch(query), DEBOUNCE_MS);
    return () => clearTimeout(timeout);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [locationValue]);

  const onSearchLocation = () => {
    const query = (locationValue ?? "").trim();
    if (!query) return;
    runSearch(query);
  };

  const onPickResult = (result: GeocodeResultDto) => {
    lastPickedRef.current = result.displayName;
    setValue("location", result.displayName);
    setSelectedCoords({ latitude: result.latitude, longitude: result.longitude });
    setResults([]);
  };

  const startEdit = (item: ItineraryItemDto) => {
    setEditingItemId(item.id);
    lastPickedRef.current = item.location;
    setSelectedCoords(item.latitude != null && item.longitude != null ? { latitude: item.latitude, longitude: item.longitude } : null);
    setResults([]);
    reset({
      title: item.title,
      description: item.description ?? "",
      type: String(item.type) as FormValues["type"],
      itemDate: item.itemDate,
      startTime: formatTime(item.startTime) ?? "",
      endTime: formatTime(item.endTime) ?? "",
      location: item.location ?? "",
    });
  };

  const cancelEdit = () => {
    setEditingItemId(null);
    lastPickedRef.current = null;
    setSelectedCoords(null);
    setResults([]);
    reset({ title: "", description: "", type: "0", itemDate: new Date().toISOString().slice(0, 10), startTime: "", endTime: "", location: "" });
  };

  const onSubmit = async (values: FormValues) => {
    const input = {
      title: values.title,
      description: values.description || null,
      type: Number(values.type) as ItineraryItemType,
      itemDate: values.itemDate,
      startTime: toTimeOnly(values.startTime),
      endTime: toTimeOnly(values.endTime),
      location: values.location || null,
      latitude: selectedCoords?.latitude ?? null,
      longitude: selectedCoords?.longitude ?? null,
    };

    if (editingItemId) {
      await updateItem.mutateAsync({ itemId: editingItemId, input });
      setEditingItemId(null);
    } else {
      await createItem.mutateAsync(input);
    }

    reset({ title: "", description: "", type: values.type, itemDate: values.itemDate, startTime: "", endTime: "", location: "" });
    lastPickedRef.current = null;
    setSelectedCoords(null);
    setResults([]);
  };

  if (isLoading) {
    return (
      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Roteiro</h2>
        <SkeletonLines />
      </Card>
    );
  }

  const sortedItems = [...(items ?? [])].sort(
    (a, b) => a.itemDate.localeCompare(b.itemDate) || (a.startTime ?? "").localeCompare(b.startTime ?? ""),
  );

  const groupedByDay: [string, ItineraryItemDto[]][] = [];
  for (const item of sortedItems) {
    const lastGroup = groupedByDay.at(-1);
    if (lastGroup && lastGroup[0] === item.itemDate) {
      lastGroup[1].push(item);
    } else {
      groupedByDay.push([item.itemDate, [item]]);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      {canEdit && (
        <Card>
          <h2 className="font-display mb-4 text-lg font-medium text-navy-900">
            {editingItemId ? "Editar item do roteiro" : "Novo item do roteiro"}
          </h2>
          <form onSubmit={handleSubmit(onSubmit)} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input label="Titulo" error={errors.title?.message} {...register("title")} />
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-navy-900" htmlFor="type">
                Tipo
              </label>
              <select
                id="type"
                className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                  transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
                {...register("type")}
              >
                {Object.entries(itineraryItemTypeLabels).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <Input label="Data" type="date" error={errors.itemDate?.message} {...register("itemDate")} />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Inicio" type="time" error={errors.startTime?.message} {...register("startTime")} />
              <Input label="Fim" type="time" error={errors.endTime?.message} {...register("endTime")} />
            </div>

            <div className="flex flex-col gap-1 sm:col-span-2">
              <label className="text-sm font-medium text-navy-900" htmlFor="location">
                Local
              </label>
              <div className="flex gap-2">
                <input
                  id="location"
                  placeholder="Ex: Cristo Redentor, Rio de Janeiro"
                  className="flex-1 rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                    focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
                  {...register("location")}
                />
                <div className="hidden sm:block">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={onSearchLocation}
                    isLoading={geocodeSearch.isPending}
                    disabled={!locationValue}
                  >
                    Buscar
                  </Button>
                </div>
              </div>
              {geocodeSearch.isError && <span className="text-sm text-coral-700">Nao foi possivel buscar esse endereco agora.</span>}
              {geocodeSearch.isSuccess && results.length === 0 && (
                <span className="text-sm text-navy-700/70">Nenhum resultado encontrado.</span>
              )}
              {results.length > 0 && (
                <ul className="mt-1 flex flex-col divide-y divide-cream-200 rounded-lg border border-cream-300">
                  {results.map((result, index) => (
                    <li key={index}>
                      <button
                        type="button"
                        onClick={() => onPickResult(result)}
                        className="w-full px-3 py-2 text-left text-sm text-navy-900 hover:bg-cream-100"
                      >
                        {result.displayName}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
              {selectedCoords && (
                <div className="mt-2 overflow-hidden rounded-lg border border-cream-300">
                  <MapView latitude={selectedCoords.latitude} longitude={selectedCoords.longitude} className="h-40 w-full" />
                </div>
              )}
            </div>

            <div className="sm:col-span-2">
              <Input label="Descricao (opcional)" error={errors.description?.message} {...register("description")} />
            </div>

            {(createItem.isError || updateItem.isError) && (
              <div className="sm:col-span-2">
                <Alert message={getErrorMessage(createItem.error ?? updateItem.error)} />
              </div>
            )}

            <div className="flex gap-3 sm:col-span-2">
              <Button type="submit" isLoading={isSubmitting} className="flex-1">
                {editingItemId ? "Salvar alteracoes" : "Adicionar ao roteiro"}
              </Button>
              {editingItemId && (
                <Button type="button" variant="secondary" onClick={cancelEdit}>
                  Cancelar
                </Button>
              )}
            </div>
          </form>
        </Card>
      )}

      <Card>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-display text-lg font-medium text-navy-900">Roteiro</h2>
          {sortedItems.length > 0 && (
            <div className="flex rounded-full border border-cream-300 p-0.5 text-sm">
              <button
                type="button"
                onClick={() => setViewMode("byDay")}
                className={`rounded-full px-3 py-1 font-medium transition-colors ${
                  viewMode === "byDay" ? "bg-brand-700 text-white" : "text-navy-700/70 hover:bg-cream-100"
                }`}
              >
                Por dia
              </button>
              <button
                type="button"
                onClick={() => setViewMode("all")}
                className={`rounded-full px-3 py-1 font-medium transition-colors ${
                  viewMode === "all" ? "bg-brand-700 text-white" : "text-navy-700/70 hover:bg-cream-100"
                }`}
              >
                Lista completa
              </button>
            </div>
          )}
        </div>

        {sortedItems.length === 0 && (
          <p className="flex items-center gap-2 text-sm text-navy-700/70">
            <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
            Nenhum item no roteiro ainda.
          </p>
        )}

        {sortedItems.length > 0 && viewMode === "all" && (
          <ul className="flex flex-col divide-y divide-cream-200">
            {sortedItems.map((item) => (
              <ItineraryItemRow
                key={item.id}
                item={item}
                canEdit={canEdit}
                showDate
                onEdit={startEdit}
                onDelete={(id) => deleteItem.mutate(id)}
                onShowMap={setMapModalItem}
                isDeleting={deleteItem.isPending}
              />
            ))}
          </ul>
        )}

        {sortedItems.length > 0 && viewMode === "byDay" && (
          <div className="flex flex-col gap-6">
            {groupedByDay.map(([date, dayItems], index) => (
              <div key={date}>
                <div className="mb-1 flex items-center justify-between gap-2 border-b border-brand-100 pb-2">
                  <div className="flex items-baseline gap-2">
                    <span className="rounded-full bg-brand-600 px-2.5 py-0.5 text-xs font-semibold text-white">Dia {index + 1}</span>
                    <span className="text-sm font-medium text-navy-900">{formatDate(date)}</span>
                  </div>
                  {dayItems.some((item) => item.latitude != null && item.longitude != null) && (
                    <button
                      type="button"
                      onClick={() => setDayMapDate(date)}
                      className="text-xs font-medium text-brand-700 hover:underline"
                    >
                      Ver mapa do dia
                    </button>
                  )}
                </div>
                <ul className="flex flex-col divide-y divide-cream-200">
                  {dayItems.map((item) => (
                    <ItineraryItemRow
                      key={item.id}
                      item={item}
                      canEdit={canEdit}
                      showDate={false}
                      onEdit={startEdit}
                      onDelete={(id) => deleteItem.mutate(id)}
                      onShowMap={setMapModalItem}
                      isDeleting={deleteItem.isPending}
                    />
                  ))}
                </ul>
              </div>
            ))}
          </div>
        )}
      </Card>

      {mapModalItem && mapModalItem.latitude != null && mapModalItem.longitude != null && (
        <Modal title={mapModalItem.title} onClose={() => setMapModalItem(null)}>
          <MapView
            latitude={mapModalItem.latitude}
            longitude={mapModalItem.longitude}
            label={mapModalItem.location ?? mapModalItem.title}
            className="h-[60vh] w-full"
            interactive
          />
          {mapModalItem.location && <p className="mt-3 text-sm text-navy-700/70">{mapModalItem.location}</p>}
        </Modal>
      )}

      {dayMapDate && (
        <DayMapModal
          date={dayMapDate}
          items={groupedByDay.find(([date]) => date === dayMapDate)?.[1] ?? []}
          onClose={() => setDayMapDate(null)}
        />
      )}
    </div>
  );
}
