import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import type { GeocodeResultDto, ItineraryDayWeatherDto, ItineraryItemDto, ItineraryItemType, ReservationDto, TripRole } from "../api/types";
import { Alert } from "../components/Alert";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { Input } from "../components/Input";
import { MapView } from "../components/MapView";
import { Modal } from "../components/Modal";
import { SkeletonLines } from "../components/Skeleton";
import { useCreateChecklistItem } from "../checklist/hooks";
import { ReservationFormModal } from "../reservations/ReservationFormModal";
import { ReservationSummary } from "../reservations/ReservationSummary";
import { useDeleteReservation, useReservations } from "../reservations/hooks";
import { formatDate, formatTime, itineraryItemTypeLabels } from "../utils/labels";
import { DayMapModal } from "./DayMapModal";
import { DaySummaryModal } from "./DaySummaryModal";
import { ItineraryProposalFormModal } from "./ItineraryProposalFormModal";
import {
  useConfirmItineraryProposal,
  useCreateItineraryItem,
  useDeleteItineraryItem,
  useGeocodeSearch,
  useItinerary,
  useItineraryWeather,
  useUpdateItineraryItem,
  useVoteItineraryProposal,
} from "./hooks";

/** Mostra so o que a previsao realmente traz - se faltar temperatura ou chuva prevista abaixo
 * do limiar de sugestao, o texto correspondente simplesmente nao aparece. */
function formatWeatherBadge(weather: ItineraryDayWeatherDto) {
  const temps = [weather.temperatureMaxC, weather.temperatureMinC].filter((t): t is number => t != null).map((t) => `${Math.round(t)}°`);
  const tempText = temps.length > 0 ? temps.join(" / ") : null;
  const rainText =
    weather.precipitationProbabilityPercent != null && weather.precipitationProbabilityPercent >= 30
      ? `${Math.round(weather.precipitationProbabilityPercent)}% chuva`
      : null;

  return [tempText, rainText].filter(Boolean).join(" · ") || null;
}

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

function MapPinIcon() {
  return (
    <svg viewBox="0 0 16 16" className="h-3.5 w-3.5" fill="none" aria-hidden="true">
      <path
        d="M8 14.5s5-4.3 5-8.3a5 5 0 1 0-10 0c0 4 5 8.3 5 8.3Z"
        stroke="currentColor"
        strokeWidth="1.3"
        strokeLinejoin="round"
      />
      <circle cx="8" cy="6.2" r="1.7" stroke="currentColor" strokeWidth="1.3" />
    </svg>
  );
}

/** Acao de mapa tem peso (abre um mapa, nao e so navegacao) - por Fitts's Law, ganha
 * affordance de botao e alvo de toque maior em vez de link de texto simples. */
function MapLinkButton({ onClick, children }: { onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex items-center gap-1.5 rounded-full border border-brand-200 bg-brand-50 px-3 py-1.5 text-xs
        font-semibold text-brand-700 transition-colors hover:bg-brand-100"
    >
      <MapPinIcon />
      {children}
    </button>
  );
}

function ItineraryItemRow({
  item,
  canEdit,
  showDate,
  currency,
  reservations,
  onEdit,
  onDelete,
  onShowMap,
  isDeleting,
  onAddReservation,
  onEditReservation,
  onDeleteReservation,
  isDeletingReservation,
  onLaunchExpense,
}: {
  item: ItineraryItemDto;
  canEdit: boolean;
  showDate: boolean;
  currency: string;
  reservations: ReservationDto[];
  onEdit: (item: ItineraryItemDto) => void;
  onDelete: (itemId: string) => void;
  onShowMap: (item: ItineraryItemDto) => void;
  isDeleting: boolean;
  onAddReservation: (itemId: string) => void;
  onEditReservation: (reservation: ReservationDto) => void;
  onDeleteReservation: (reservationId: string) => void;
  isDeletingReservation: boolean;
  onLaunchExpense: (reservation: ReservationDto) => void;
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
            <div className="mt-2">
              <MapLinkButton onClick={() => onShowMap(item)}>Ver no mapa</MapLinkButton>
            </div>
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

      {reservations.length > 0 && (
        <div className="flex flex-col gap-2">
          {reservations.map((reservation) => (
            <ReservationSummary
              key={reservation.id}
              reservation={reservation}
              currency={currency}
              canEdit={canEdit}
              onEdit={onEditReservation}
              onDelete={onDeleteReservation}
              isDeleting={isDeletingReservation}
              onLaunchExpense={onLaunchExpense}
            />
          ))}
        </div>
      )}

      {canEdit && (
        <button
          type="button"
          onClick={() => onAddReservation(item.id)}
          className="self-start text-xs font-medium text-brand-700 hover:underline"
        >
          + Vincular reserva
        </button>
      )}
    </li>
  );
}

/** Uma proposta (Status=Proposed) em vez do item normal - mostra as opcoes concorrentes com
 * contagem de votos em tempo real (via SignalR) em vez de local/mapa/reserva, que so fazem
 * sentido depois que uma delas vira o item confirmado de verdade. */
function ItineraryProposalCard({
  item,
  canEdit,
  showDate,
  onVote,
  onConfirm,
  isVoting,
  isConfirming,
}: {
  item: ItineraryItemDto;
  canEdit: boolean;
  showDate: boolean;
  onVote: (itemId: string, optionId: string) => void;
  onConfirm: (itemId: string, optionId: string) => void;
  isVoting: boolean;
  isConfirming: boolean;
}) {
  const metaParts = [
    showDate ? formatDate(item.itemDate) : null,
    formatTime(item.startTime)
      ? `${formatTime(item.startTime)}${formatTime(item.endTime) ? `-${formatTime(item.endTime)}` : ""}`
      : null,
  ].filter(Boolean);
  const totalVotes = item.proposalOptions.reduce((sum, option) => sum + option.voteCount, 0);

  return (
    <li className="flex flex-col gap-2 rounded-xl border border-dashed border-amber-300 bg-amber-50/40 p-3">
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
        <p className="text-sm font-medium text-navy-900">{item.title}</p>
        <Badge tone="warning">Em votacao</Badge>
        <Badge tone="neutral">{itineraryItemTypeLabels[item.type]}</Badge>
        {metaParts.length > 0 && <span className="text-xs text-navy-700/50">{metaParts.join(" · ")}</span>}
      </div>

      <ul className="flex flex-col gap-2">
        {item.proposalOptions.map((option) => {
          const isMine = item.myVotedOptionId === option.id;
          const percent = totalVotes > 0 ? Math.round((option.voteCount / totalVotes) * 100) : 0;
          return (
            <li key={option.id} className="rounded-lg border border-cream-300 bg-white p-2.5">
              <div className="flex items-center justify-between gap-2">
                <button
                  type="button"
                  onClick={() => onVote(item.id, option.id)}
                  disabled={isVoting || isMine}
                  className={`flex-1 text-left text-sm font-medium ${isMine ? "text-brand-700" : "text-navy-900 hover:text-brand-700"}`}
                >
                  {option.title}
                  {isMine && <span className="ml-1.5 text-xs font-normal text-brand-600">(seu voto)</span>}
                </button>
                <span className="shrink-0 text-xs text-navy-700/60">
                  {option.voteCount} {option.voteCount === 1 ? "voto" : "votos"}
                </span>
              </div>
              {option.location && <p className="mt-0.5 text-xs text-navy-700/50">{option.location}</p>}
              <div className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-cream-200">
                <div className="h-full rounded-full bg-brand-500" style={{ width: `${percent}%` }} />
              </div>
              {canEdit && (
                <button
                  type="button"
                  onClick={() => onConfirm(item.id, option.id)}
                  disabled={isConfirming}
                  className="mt-1.5 text-xs font-medium text-brand-700 hover:underline disabled:opacity-50"
                >
                  Confirmar esta opcao
                </button>
              )}
            </li>
          );
        })}
      </ul>
    </li>
  );
}

export function ItineraryPanel({
  tripId,
  myRole,
  currency,
  isOngoing = false,
  onLaunchExpense,
}: {
  tripId: string;
  myRole: TripRole | undefined;
  currency: string;
  isOngoing?: boolean;
  onLaunchExpense: (reservation: ReservationDto) => void;
}) {
  const { data: items, isLoading } = useItinerary(tripId);
  const createItem = useCreateItineraryItem(tripId);
  const updateItem = useUpdateItineraryItem(tripId);
  const deleteItem = useDeleteItineraryItem(tripId);
  const geocodeSearch = useGeocodeSearch();

  const { data: reservations } = useReservations(tripId);
  const deleteReservation = useDeleteReservation(tripId);

  const { data: weatherForecast } = useItineraryWeather(tripId);
  const createChecklistItem = useCreateChecklistItem(tripId);
  const [dismissedSuggestions, setDismissedSuggestions] = useState<Set<string>>(new Set());
  const weatherByDate = new Map((weatherForecast ?? []).map((weather) => [weather.date, weather]));

  const addSuggestedChecklistItem = (date: string, suggestion: string) => {
    createChecklistItem.mutate(
      { title: suggestion, assignedToParticipantId: null, dueDate: date },
      { onSuccess: () => setDismissedSuggestions((prev) => new Set(prev).add(`${date}:${suggestion}`)) },
    );
  };

  const [isProposalModalOpen, setIsProposalModalOpen] = useState(false);
  const voteOnProposal = useVoteItineraryProposal(tripId);
  const confirmProposal = useConfirmItineraryProposal(tripId);

  const [results, setResults] = useState<GeocodeResultDto[]>([]);
  const [selectedCoords, setSelectedCoords] = useState<{ latitude: number; longitude: number } | null>(null);
  const [mapModalItem, setMapModalItem] = useState<ItineraryItemDto | null>(null);
  const [dayMapDate, setDayMapDate] = useState<string | null>(null);
  const [daySummaryDate, setDaySummaryDate] = useState<string | null>(null);
  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<ViewMode>("byDay");
  const [reservationModal, setReservationModal] = useState<{ lockedItineraryItemId?: string; initial?: ReservationDto } | null>(
    null,
  );
  const [typeFilter, setTypeFilter] = useState("");
  const [searchFilter, setSearchFilter] = useState("");
  const lastPickedRef = useRef<string | null>(null);
  const searchIdRef = useRef(0);

  const canEdit = myRole === 1 || myRole === 2;
  const reservationsByItemId = new Map<string, ReservationDto[]>();
  const unlinkedReservations: ReservationDto[] = [];
  for (const reservation of reservations ?? []) {
    if (reservation.itineraryItemId) {
      const list = reservationsByItemId.get(reservation.itineraryItemId) ?? [];
      list.push(reservation);
      reservationsByItemId.set(reservation.itineraryItemId, list);
    } else {
      unlinkedReservations.push(reservation);
    }
  }

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

  // So pro que aparece na tela - o modal de reserva continua usando sortedItems (lista
  // completa) pro select de "vincular a um item", independente do filtro aplicado aqui.
  const filteredItems = sortedItems.filter((item) => {
    if (typeFilter !== "" && String(item.type) !== typeFilter) return false;

    const term = searchFilter.trim().toLowerCase();
    if (!term) return true;

    return item.title.toLowerCase().includes(term) || (item.location ?? "").toLowerCase().includes(term);
  });
  const hasItineraryFilters = typeFilter !== "" || searchFilter.trim() !== "";

  const todayIso = new Date().toISOString().slice(0, 10);

  const groupedByDay: [string, ItineraryItemDto[]][] = [];
  for (const item of filteredItems) {
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
              {editingItemId ? (
                <Button type="button" variant="secondary" onClick={cancelEdit}>
                  Cancelar
                </Button>
              ) : (
                <Button type="button" variant="secondary" onClick={() => setIsProposalModalOpen(true)}>
                  Propor opcoes
                </Button>
              )}
            </div>
          </form>
        </Card>
      )}

      {isProposalModalOpen && <ItineraryProposalFormModal tripId={tripId} onClose={() => setIsProposalModalOpen(false)} />}

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

        {sortedItems.length > 0 && (
          <div className="mb-4 flex flex-col gap-3 sm:flex-row">
            <input
              type="text"
              placeholder="Buscar por titulo ou local"
              value={searchFilter}
              onChange={(e) => setSearchFilter(e.target.value)}
              className="flex-1 rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
            />
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
            >
              <option value="">Todos os tipos</option>
              {Object.entries(itineraryItemTypeLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
            {hasItineraryFilters && (
              <button
                type="button"
                onClick={() => {
                  setTypeFilter("");
                  setSearchFilter("");
                }}
                className="shrink-0 self-center text-xs font-medium text-brand-700 hover:underline"
              >
                Limpar filtros
              </button>
            )}
          </div>
        )}

        {filteredItems.length === 0 && (
          <p className="flex items-center gap-2 text-sm text-navy-700/70">
            <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
            {hasItineraryFilters ? "Nenhum item encontrado com esses filtros." : "Nenhum item no roteiro ainda."}
          </p>
        )}

        {filteredItems.length > 0 && viewMode === "all" && (
          <ul className="flex flex-col gap-2 divide-y divide-cream-200">
            {filteredItems.map((item) =>
              item.status === 1 ? (
                <ItineraryProposalCard
                  key={item.id}
                  item={item}
                  canEdit={canEdit}
                  showDate
                  onVote={(itemId, optionId) => voteOnProposal.mutate({ itemId, optionId })}
                  onConfirm={(itemId, optionId) => confirmProposal.mutate({ itemId, optionId })}
                  isVoting={voteOnProposal.isPending}
                  isConfirming={confirmProposal.isPending}
                />
              ) : (
                <ItineraryItemRow
                  key={item.id}
                  item={item}
                  canEdit={canEdit}
                  showDate
                  currency={currency}
                  reservations={reservationsByItemId.get(item.id) ?? []}
                  onEdit={startEdit}
                  onDelete={(id) => deleteItem.mutate(id)}
                  onShowMap={setMapModalItem}
                  isDeleting={deleteItem.isPending}
                  onAddReservation={(itemId) => setReservationModal({ lockedItineraryItemId: itemId })}
                  onEditReservation={(reservation) => setReservationModal({ initial: reservation })}
                  onDeleteReservation={(id) => deleteReservation.mutate(id)}
                  isDeletingReservation={deleteReservation.isPending}
                  onLaunchExpense={onLaunchExpense}
                />
              ),
            )}
          </ul>
        )}

        {filteredItems.length > 0 && viewMode === "byDay" && (
          <div className="flex flex-col gap-6">
            {groupedByDay.map(([date, dayItems], index) => {
              const isToday = isOngoing && date === todayIso;
              const dayWeather = weatherByDate.get(date);
              const weatherBadgeText = dayWeather ? formatWeatherBadge(dayWeather) : null;
              const pendingSuggestions = (dayWeather?.suggestedChecklistItems ?? []).filter(
                (suggestion) => !dismissedSuggestions.has(`${date}:${suggestion}`),
              );
              return (
              <div key={date} className={isToday ? "-mx-3 rounded-xl border border-brand-200 bg-brand-50/40 px-3 py-2" : undefined}>
                <div className="mb-1 flex flex-col gap-2 border-b border-brand-100 pb-2">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="flex flex-wrap items-baseline gap-2">
                      <span className="rounded-full bg-brand-600 px-2.5 py-0.5 text-xs font-semibold text-white">Dia {index + 1}</span>
                      <span className="text-sm font-medium text-navy-900">{formatDate(date)}</span>
                      {isToday && <Badge tone="success">Hoje</Badge>}
                      {weatherBadgeText && (
                        <Badge tone={dayWeather && (dayWeather.precipitationProbabilityPercent ?? 0) >= 50 ? "warning" : "neutral"}>
                          {weatherBadgeText}
                        </Badge>
                      )}
                    </div>
                    <div className="flex items-center gap-3">
                      <button
                        type="button"
                        onClick={() => setDaySummaryDate(date)}
                        className="text-xs font-medium text-brand-700 hover:underline"
                      >
                        Resumo do dia
                      </button>
                      {dayItems.some((item) => item.latitude != null && item.longitude != null) && (
                        <MapLinkButton onClick={() => setDayMapDate(date)}>Ver mapa do dia</MapLinkButton>
                      )}
                    </div>
                  </div>

                  {canEdit && pendingSuggestions.length > 0 && (
                    <div className="flex flex-col gap-1.5">
                      {pendingSuggestions.map((suggestion) => (
                        <Alert
                          key={suggestion}
                          variant="info"
                          message={`Previsao pede atencao nesse dia - adicionar "${suggestion}" ao checklist?`}
                          action={
                            <Button
                              variant="secondary"
                              disabled={createChecklistItem.isPending}
                              onClick={() => addSuggestedChecklistItem(date, suggestion)}
                            >
                              Adicionar
                            </Button>
                          }
                        />
                      ))}
                    </div>
                  )}
                </div>
                <ul className="flex flex-col gap-2 divide-y divide-cream-200">
                  {dayItems.map((item) =>
                    item.status === 1 ? (
                      <ItineraryProposalCard
                        key={item.id}
                        item={item}
                        canEdit={canEdit}
                        showDate={false}
                        onVote={(itemId, optionId) => voteOnProposal.mutate({ itemId, optionId })}
                        onConfirm={(itemId, optionId) => confirmProposal.mutate({ itemId, optionId })}
                        isVoting={voteOnProposal.isPending}
                        isConfirming={confirmProposal.isPending}
                      />
                    ) : (
                      <ItineraryItemRow
                        key={item.id}
                        item={item}
                        canEdit={canEdit}
                        showDate={false}
                        currency={currency}
                        reservations={reservationsByItemId.get(item.id) ?? []}
                        onEdit={startEdit}
                        onDelete={(id) => deleteItem.mutate(id)}
                        onShowMap={setMapModalItem}
                        isDeleting={deleteItem.isPending}
                        onAddReservation={(itemId) => setReservationModal({ lockedItineraryItemId: itemId })}
                        onEditReservation={(reservation) => setReservationModal({ initial: reservation })}
                        onDeleteReservation={(id) => deleteReservation.mutate(id)}
                        isDeletingReservation={deleteReservation.isPending}
                        onLaunchExpense={onLaunchExpense}
                      />
                    ),
                  )}
                </ul>
              </div>
              );
            })}
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

      {daySummaryDate && (
        <DaySummaryModal
          date={daySummaryDate}
          items={groupedByDay.find(([date]) => date === daySummaryDate)?.[1] ?? []}
          reservationsByItemId={reservationsByItemId}
          currency={currency}
          onClose={() => setDaySummaryDate(null)}
        />
      )}

      <Card>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-display text-lg font-medium text-navy-900">Reservas sem item vinculado</h2>
          {canEdit && (
            <Button variant="secondary" onClick={() => setReservationModal({})}>
              Nova reserva
            </Button>
          )}
        </div>

        {unlinkedReservations.length === 0 ? (
          <p className="flex items-center gap-2 text-sm text-navy-700/70">
            <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
            Nenhuma reserva avulsa - reservas de voo, hospedagem ou carro que nao estao ligadas a um item do roteiro aparecem aqui.
          </p>
        ) : (
          <div className="flex flex-col gap-2">
            {unlinkedReservations.map((reservation) => (
              <ReservationSummary
                key={reservation.id}
                reservation={reservation}
                currency={currency}
                canEdit={canEdit}
                onEdit={(r) => setReservationModal({ initial: r })}
                onDelete={(id) => deleteReservation.mutate(id)}
                isDeleting={deleteReservation.isPending}
                onLaunchExpense={onLaunchExpense}
              />
            ))}
          </div>
        )}
      </Card>

      {reservationModal && (
        <ReservationFormModal
          tripId={tripId}
          itineraryItems={sortedItems.filter((item) => item.status !== 1)}
          lockedItineraryItemId={reservationModal.lockedItineraryItemId}
          initial={reservationModal.initial ?? null}
          onClose={() => setReservationModal(null)}
        />
      )}
    </div>
  );
}
