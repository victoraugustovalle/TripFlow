import { getAccessToken, useAuthStore } from "../auth/authStore";
import type { AccessTokenResponse } from "./types";

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

export class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;

  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message);
    this.status = status;
    this.errors = errors;
  }
}

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  /** Login/registro/refresh nao mandam o header Authorization - nao tem token ainda, ou o proprio refresh nao pode depender de um token que pode estar expirado. */
  skipAuth?: boolean;
  /** Uso interno - evita loop infinito se o refresh nao resolver o 401. */
  isRetry?: boolean;
}

let refreshInFlight: Promise<boolean> | null = null;

/**
 * So um refresh por vez mesmo se varias chamadas tomarem 401 ao mesmo tempo (ex: a tela
 * carrega viagem + participantes + gastos juntos e o token expirou entre uma e outra) -
 * sem isso, cada uma dispararia seu proprio /refresh e a rotacao de refresh token
 * invalidaria as outras por reuso.
 */
function refreshAccessToken(): Promise<boolean> {
  if (!refreshInFlight) {
    refreshInFlight = doRefresh().finally(() => {
      refreshInFlight = null;
    });
  }
  return refreshInFlight;
}

async function doRefresh(): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: "POST",
      credentials: "include",
    });

    if (!response.ok) {
      useAuthStore.getState().clearSession();
      return false;
    }

    const data = (await response.json()) as AccessTokenResponse;
    useAuthStore.getState().setSession(data.accessToken, data.user);
    return true;
  } catch {
    useAuthStore.getState().clearSession();
    return false;
  }
}

async function safeParseJson(response: Response): Promise<{ message?: string; errors?: Record<string, string[]> } | null> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = "GET", body, skipAuth = false, isRetry = false } = options;

  const headers: Record<string, string> = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";

  const token = getAccessToken();
  if (token && !skipAuth) headers["Authorization"] = `Bearer ${token}`;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    credentials: "include",
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (response.status === 401 && !skipAuth && !isRetry) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      return apiFetch<T>(path, { ...options, isRetry: true });
    }
  }

  if (!response.ok) {
    const problem = await safeParseJson(response);
    throw new ApiError(response.status, problem?.message ?? "Erro inesperado.", problem?.errors);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export { doRefresh };
