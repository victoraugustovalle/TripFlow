import type { ActivityType, DocumentCategory, ItineraryItemType, NotificationType, ParticipantStatus, ReservationType, TripRole, TripStatus } from "../api/types";
import type { BadgeTone } from "../components/Badge";

export const notificationTypeLabels: Record<NotificationType, string> = {
  0: "Participante entrou na viagem",
  1: "Item do checklist atribuido a voce",
  2: "Gasto lancado",
  3: "Orcamento estourado",
  4: "Reserva criada",
  5: "Reserva atualizada",
  6: "Item do roteiro atualizado",
  7: "Documento removido",
  8: "Quitacao marcada como paga",
  9: "Quitacao confirmada",
  10: "Viagem chegando e ainda nao esta pronta",
  11: "Orcamento no ritmo de estourar",
};

/** Disparadas pelo job diario (ProactiveNotificationsHostedService), nao por uma acao de outro
 * participante - a UI marca essas com um selo pra deixar claro que "ninguem fez nada", foi o
 * sistema que percebeu sozinho. */
export const proactiveNotificationTypes = new Set<NotificationType>([10, 11]);

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

export const reservationTypeLabels: Record<ReservationType, string> = {
  0: "Voo",
  1: "Hospedagem",
  2: "Aluguel de carro",
  3: "Outro",
};

export const documentCategoryLabels: Record<DocumentCategory, string> = {
  0: "Passaporte",
  1: "Passagem",
  2: "Voucher",
  3: "Seguro",
  4: "Outro",
};

export const activityTypeLabels: Record<ActivityType, string> = {
  0: "Gasto",
  1: "Gasto",
  2: "Checklist",
  3: "Checklist",
  4: "Checklist",
  5: "Roteiro",
  6: "Roteiro",
  7: "Reserva",
  8: "Reserva",
  9: "Participante",
};

/** Criacao/entrada = positivo, remocao = atencao, atribuicao = neutro-informativo. */
export const activityTypeTone: Record<ActivityType, BadgeTone> = {
  0: "success",
  1: "danger",
  2: "success",
  3: "warning",
  4: "danger",
  5: "success",
  6: "danger",
  7: "success",
  8: "danger",
  9: "success",
};

export function formatFileSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

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

/** So faz sentido pra viagem ainda em planejamento com data de inicio definida - depois que
 * comeca (Ongoing) ou termina, contagem regressiva perde o sentido. */
export function formatCountdown(startDate: string | null, status: TripStatus): string | null {
  if (status !== 0 || !startDate) return null;

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const start = new Date(startDate);
  start.setHours(0, 0, 0, 0);

  const diffDays = Math.round((start.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

  if (diffDays < 0) return null;
  if (diffDays === 0) return "Comeca hoje";
  if (diffDays === 1) return "Falta 1 dia";
  return `Faltam ${diffDays} dias`;
}

export function formatDateTime(value: string | null) {
  if (!value) return null;
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}
