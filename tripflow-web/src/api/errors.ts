import { ApiError } from "./client";

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.errors) {
      const firstField = Object.values(error.errors)[0];
      if (firstField?.[0]) return firstField[0];
    }
    return error.message;
  }

  if (error instanceof Error) return error.message;
  return "Erro inesperado.";
}
