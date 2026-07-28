import { create } from "zustand";
import type { UserDto } from "../api/types";

interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  /** "loading" enquanto tenta o refresh silencioso no boot do app - evita piscar a tela de login. */
  status: "loading" | "authenticated" | "unauthenticated";
  setSession: (accessToken: string, user: UserDto) => void;
  clearSession: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  status: "loading",
  setSession: (accessToken, user) => set({ accessToken, user, status: "authenticated" }),
  clearSession: () => set({ accessToken: null, user: null, status: "unauthenticated" }),
}));

/** Pro cliente de API ler o token atual fora de componentes React (interceptor de fetch). */
export function getAccessToken(): string | null {
  return useAuthStore.getState().accessToken;
}
