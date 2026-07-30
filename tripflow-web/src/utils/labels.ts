import type { ItineraryItemType, ParticipantStatus, TripRole, TripStatus } from "../api/types";
import type { BadgeTone } from "../components/Badge";

export const tripRoleLabels: Record<TripRole, string> = {
  0: "Visualizador",
  1: "Editor",
  2: "Dono",
};

/** Planejando = ainda em aberto (atencao); Em andamento = acontecendo agora (positivo);
 * Concluida = encerrada sem carga emocional (neutro); Cancelada = negativo. */
export const tripStatusTone: Record<TripStatus, BadgeTone> = {
  0: "warning",
  1: "success",
  2: "neutral",
  3: "danger",
};

export const participantStatusLabels: Record<ParticipantStatus, string> = {
  0: "Convidado",
  1: "Aceito",
  2: "Recusado",
};

export const participantStatusTone: Record<ParticipantStatus, BadgeTone> = {
  0: "warning",
  1: "success",
  2: "danger",
};

export const itineraryItemTypeLabels: Record<ItineraryItemType, string> = {
  0: "Atividade",
  1: "Transporte",
  2: "Hospedagem",
  3: "Refeicao",
  4: "Outro",
};

export const tripStatusLabels: Record<TripStatus, string> = {
  0: "Planejando",
  1: "Em andamento",
  2: "Concluida",
  3: "Cancelada",
};

export function formatCurrency(amount: number, currency = "BRL") {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency }).format(amount);
}

export function formatDate(value: string | null) {
  if (!value) return "-";
  return new Intl.DateTimeFormat("pt-BR").format(new Date(value));
}

export function formatTime(value: string | null) {
  if (!value) return null;
  return value.slice(0, 5);
}
