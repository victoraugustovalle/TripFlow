import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import type { ItineraryItemDto, ReservationDto } from "../api/types";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { Modal } from "../components/Modal";
import { formatDate, reservationTypeLabels } from "../utils/labels";
import { useCreateReservation, useUpdateReservation } from "./hooks";

const schema = z.object({
  type: z.enum(["0", "1", "2", "3"]),
  title: z.string().min(1, "Informe um titulo."),
  providerName: z.string().optional(),
  confirmationCode: z.string().optional(),
  startDate: z.string().optional(),
  startTime: z.string().optional(),
  endDate: z.string().optional(),
  endTime: z.string().optional(),
  location: z.string().optional(),
  price: z.string().optional(),
  currency: z.string().optional(),
  notes: z.string().optional(),
  itineraryItemId: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

function splitIso(value: string | null) {
  if (!value) return { date: "", time: "" };
  return { date: value.slice(0, 10), time: value.slice(11, 16) };
}

function toIsoDateTime(date: string | undefined, time: string | undefined) {
  if (!date) return null;
  return `${date}T${time || "00:00"}:00`;
}

export function ReservationFormModal({
  tripId,
  itineraryItems,
  lockedItineraryItemId,
  initial,
  onClose,
}: {
  tripId: string;
  itineraryItems: ItineraryItemDto[];
  lockedItineraryItemId?: string;
  initial?: ReservationDto | null;
  onClose: () => void;
}) {
  const createReservation = useCreateReservation(tripId);
  const updateReservation = useUpdateReservation(tripId);

  const start = splitIso(initial?.startAt ?? null);
  const end = splitIso(initial?.endAt ?? null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      type: initial ? (String(initial.type) as FormValues["type"]) : "0",
      title: initial?.title ?? "",
      providerName: initial?.providerName ?? "",
      confirmationCode: initial?.confirmationCode ?? "",
      startDate: start.date,
      startTime: start.time,
      endDate: end.date,
      endTime: end.time,
      location: initial?.location ?? "",
      price: initial?.price != null ? String(initial.price) : "",
      currency: initial?.currency ?? "",
      notes: initial?.notes ?? "",
      itineraryItemId: lockedItineraryItemId ?? initial?.itineraryItemId ?? "",
    },
  });

  const onSubmit = async (values: FormValues) => {
    const input = {
      type: Number(values.type) as ReservationDto["type"],
      title: values.title,
      providerName: values.providerName || null,
      confirmationCode: values.confirmationCode || null,
      startAt: toIsoDateTime(values.startDate, values.startTime),
      endAt: toIsoDateTime(values.endDate, values.endTime),
      location: values.location || null,
      latitude: null,
      longitude: null,
      price: values.price ? Number(values.price) : null,
      currency: values.currency || null,
      notes: values.notes || null,
      itineraryItemId: lockedItineraryItemId ?? (values.itineraryItemId || null),
    };

    if (initial) {
      await updateReservation.mutateAsync({ reservationId: initial.id, input });
    } else {
      await createReservation.mutateAsync(input);
    }
    onClose();
  };

  const mutation = initial ? updateReservation : createReservation;

  return (
    <Modal title={initial ? "Editar reserva" : "Nova reserva"} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-navy-900" htmlFor="reservation-type">
            Tipo
          </label>
          <select
            id="reservation-type"
            className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
              transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
            {...register("type")}
          >
            {Object.entries(reservationTypeLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>

        <Input label="Titulo" error={errors.title?.message} {...register("title")} />
        <Input label="Fornecedor (opcional)" {...register("providerName")} />
        <Input label="Codigo de confirmacao (opcional)" {...register("confirmationCode")} />

        <div className="grid grid-cols-2 gap-4">
          <Input label="Inicio - data" type="date" {...register("startDate")} />
          <Input label="Inicio - hora" type="time" {...register("startTime")} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Fim - data" type="date" {...register("endDate")} />
          <Input label="Fim - hora" type="time" {...register("endTime")} />
        </div>

        <Input label="Local (opcional)" {...register("location")} />
        <div className="grid grid-cols-2 gap-4">
          <Input label="Preco (opcional)" type="number" step="0.01" min="0" {...register("price")} />
          <Input label="Moeda (opcional)" placeholder="BRL" {...register("currency")} />
        </div>

        <div className="sm:col-span-2">
          <Input label="Notas (opcional)" {...register("notes")} />
        </div>

        {!lockedItineraryItemId && (
          <div className="flex flex-col gap-1 sm:col-span-2">
            <label className="text-sm font-medium text-navy-900" htmlFor="reservation-itinerary-item">
              Vincular a um item do roteiro (opcional)
            </label>
            <select
              id="reservation-itinerary-item"
              className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
              {...register("itineraryItemId")}
            >
              <option value="">Nenhum</option>
              {itineraryItems.map((item) => (
                <option key={item.id} value={item.id}>
                  {formatDate(item.itemDate)} · {item.title}
                </option>
              ))}
            </select>
          </div>
        )}

        {mutation.isError && (
          <div className="sm:col-span-2">
            <Alert message={getErrorMessage(mutation.error)} />
          </div>
        )}

        <div className="flex justify-end gap-3 sm:col-span-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {initial ? "Salvar alteracoes" : "Adicionar reserva"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
