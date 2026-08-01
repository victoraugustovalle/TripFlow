import { create } from "zustand";
import type { ExpenseDraft } from "./reservationToExpenseDraft";

interface ExpenseDraftState {
  draft: ExpenseDraft | null;
  setDraft: (draft: ExpenseDraft) => void;
  clearDraft: () => void;
}

/** Ponte entre "Lancar como gasto" (na reserva, aba Roteiro) e o formulario de novo gasto (aba
 * Gastos) - as duas abas sao paineis irmaos que desmontam/remontam ao trocar de aba
 * (TripDetailPage), entao nao da pra passar isso por prop; o ExpensesPanel le o draft uma vez
 * no mount e limpa em seguida. */
export const useExpenseDraftStore = create<ExpenseDraftState>((set) => ({
  draft: null,
  setDraft: (draft) => set({ draft }),
  clearDraft: () => set({ draft: null }),
}));
