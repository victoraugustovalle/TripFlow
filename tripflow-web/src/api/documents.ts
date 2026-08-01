import { getAccessToken } from "../auth/authStore";
import { API_BASE_URL, ApiError, apiFetch, refreshAccessToken } from "./client";
import type { DocumentCategory, DocumentDto, PagedResult } from "./types";

export function listDocuments(tripId: string, page = 1, pageSize = 20) {
  return apiFetch<PagedResult<DocumentDto>>(`/api/trips/${tripId}/documents?page=${page}&pageSize=${pageSize}`);
}

export function deleteDocument(tripId: string, documentId: string) {
  return apiFetch<void>(`/api/trips/${tripId}/documents/${documentId}`, { method: "DELETE" });
}

async function parseErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { message?: string };
    return body.message ?? "Erro inesperado.";
  } catch {
    return "Erro inesperado.";
  }
}

/**
 * Upload e download de documento nao passam pelo apiFetch: upload manda multipart/form-data
 * (o browser precisa gerar o boundary sozinho, nao da pra fixar Content-Type na mao) e
 * download devolve bytes binarios, nao JSON. Os dois ainda precisam do mesmo tratamento de
 * token (header Authorization + um retry de refresh no 401) que o apiFetch faz.
 */
export async function uploadDocument(
  tripId: string,
  file: File,
  category: DocumentCategory,
  isRetry = false,
): Promise<DocumentDto> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("category", String(category));

  const token = getAccessToken();
  const response = await fetch(`${API_BASE_URL}/api/trips/${tripId}/documents`, {
    method: "POST",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    credentials: "include",
    body: formData,
  });

  if (response.status === 401 && !isRetry) {
    const refreshed = await refreshAccessToken();
    if (refreshed) return uploadDocument(tripId, file, category, true);
  }

  if (!response.ok) {
    throw new ApiError(response.status, await parseErrorMessage(response));
  }

  return response.json();
}

export async function downloadDocument(
  tripId: string,
  documentId: string,
  fileName: string,
  isRetry = false,
): Promise<void> {
  const token = getAccessToken();
  const response = await fetch(`${API_BASE_URL}/api/trips/${tripId}/documents/${documentId}/download`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    credentials: "include",
  });

  if (response.status === 401 && !isRetry) {
    const refreshed = await refreshAccessToken();
    if (refreshed) return downloadDocument(tripId, documentId, fileName, true);
  }

  if (!response.ok) {
    throw new ApiError(response.status, await parseErrorMessage(response));
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}
