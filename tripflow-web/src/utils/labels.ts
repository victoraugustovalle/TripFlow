import type { TripRole, TripStatus } from "../api/types";

export const tripRoleLabels: Record<TripRole, string> = {
  0: "Visualizador",
  1: "Editor",
  2: "Dono",
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
