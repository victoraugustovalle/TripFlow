import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as documentsApi from "../api/documents";
import type { DocumentCategory } from "../api/types";
import { pushToast } from "../toast/toastStore";

export function useDocuments(tripId: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["documents", tripId, page, pageSize],
    queryFn: () => documentsApi.listDocuments(tripId, page, pageSize),
    placeholderData: keepPreviousData,
  });
}

export function useUploadDocument(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ file, category }: { file: File; category: DocumentCategory }) =>
      documentsApi.uploadDocument(tripId, file, category),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["documents", tripId] });
      pushToast("Documento enviado.");
    },
  });
}

export function useDeleteDocument(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => documentsApi.deleteDocument(tripId, documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["documents", tripId] });
      pushToast("Documento removido.");
    },
  });
}
