import type { ReservationDto, ReservationType } from "../api/types";

/** Categoria sugerida a partir do tipo da reserva - so um ponto de partida editavel, nao uma taxonomia fixa (Category e texto livre no backend). */
const categoryByReservationType: Record<ReservationType, string> = {
  0: "Passagens", // Flight
  1: "Hospedagem", // Hotel
  2: "Transporte", // CarRental
  3: "Geral", // Other
};

export interface ExpenseDraft {
  description: string;
  amount: number;
  category: string;
  expenseDate: string;
}

export function reservationToExpenseDraft(reservation: ReservationDto): ExpenseDraft {
  return {
    description: reservation.title,
    amount: reservation.price ?? 0,
    category: categoryByReservationType[reservation.type],
    expenseDate: reservation.startAt ? reservation.startAt.slice(0, 10) : new Date().toISOString().slice(0, 10),
  };
}
