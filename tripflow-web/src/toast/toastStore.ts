import { create } from "zustand";

export interface Toast {
  id: number;
  message: string;
  /** "info" e pra eventos de tempo real (algo que outra pessoa fez) - visualmente distinto de
   * "success"/"error", que sao sempre feedback da sua propria acao. */
  variant: "success" | "error" | "info";
}

interface ToastState {
  toasts: Toast[];
  dismiss: (id: number) => void;
}

let nextId = 1;

export const useToastStore = create<ToastState>((set) => ({
  toasts: [],
  dismiss: (id) => set((s) => ({ toasts: s.toasts.filter((t) => t.id !== id) })),
}));

/** Chamavel fora de componentes (ex.: onSuccess de mutations do React Query). */
export function pushToast(message: string, variant: Toast["variant"] = "success") {
  const id = nextId++;
  useToastStore.setState((s) => ({ toasts: [...s.toasts, { id, message, variant }] }));
  return id;
}
