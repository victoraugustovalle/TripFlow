import { useRef, useState } from "react";
import { getErrorMessage } from "../api/errors";
import { downloadDocument } from "../api/documents";
import type { DocumentCategory, TripRole } from "../api/types";
import { Alert } from "../components/Alert";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { SkeletonLines } from "../components/Skeleton";
import { pushToast } from "../toast/toastStore";
import { documentCategoryLabels, formatDateTime, formatFileSize } from "../utils/labels";
import { useDeleteDocument, useDocuments, useUploadDocument } from "./hooks";

const PAGE_SIZE = 20;

export function DocumentsPanel({ tripId, myRole }: { tripId: string; myRole: TripRole | undefined }) {
  const [page, setPage] = useState(1);
  const { data: documentsPage, isLoading, isPlaceholderData } = useDocuments(tripId, page, PAGE_SIZE);
  const uploadDocument = useUploadDocument(tripId);
  const deleteDocument = useDeleteDocument(tripId);
  const canEdit = myRole === 1 || myRole === 2;

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [category, setCategory] = useState<DocumentCategory>(4);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  const totalPages = documentsPage ? Math.max(1, Math.ceil(documentsPage.totalCount / documentsPage.pageSize)) : 1;

  const handleUpload = async () => {
    const file = fileInputRef.current?.files?.[0];
    if (!file) return;
    await uploadDocument.mutateAsync({ file, category });
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handleDownload = async (documentId: string, fileName: string) => {
    setDownloadingId(documentId);
    try {
      await downloadDocument(tripId, documentId, fileName);
    } catch (error) {
      pushToast(getErrorMessage(error));
    } finally {
      setDownloadingId(null);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Documentos</h2>
        <SkeletonLines />
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      {canEdit && (
        <Card>
          <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Enviar documento</h2>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void handleUpload();
            }}
            className="grid grid-cols-1 gap-4 sm:grid-cols-[1fr_auto_auto]"
          >
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-navy-900" htmlFor="document-file">
                Arquivo
              </label>
              <input
                id="document-file"
                ref={fileInputRef}
                type="file"
                accept=".pdf,.jpg,.jpeg,.png,.webp"
                className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                  file:mr-3 file:rounded-full file:border-0 file:bg-cream-200 file:px-3 file:py-1 file:text-xs file:font-medium file:text-navy-900"
              />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-navy-900" htmlFor="document-category">
                Categoria
              </label>
              <select
                id="document-category"
                value={category}
                onChange={(e) => setCategory(Number(e.target.value) as DocumentCategory)}
                className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                  transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
              >
                {Object.entries(documentCategoryLabels).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-end">
              <Button type="submit" isLoading={uploadDocument.isPending} className="w-full sm:w-auto">
                Enviar
              </Button>
            </div>

            {uploadDocument.isError && (
              <div className="sm:col-span-3">
                <Alert message={getErrorMessage(uploadDocument.error)} />
              </div>
            )}
          </form>
          <p className="mt-2 text-xs text-navy-700/50">PDF, JPEG, PNG ou WEBP, ate 10MB.</p>
        </Card>
      )}

      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Documentos</h2>

        {documentsPage?.items.length === 0 && (
          <p className="flex items-center gap-2 text-sm text-navy-700/70">
            <FlightTrail className="h-5 w-8 shrink-0 text-brand-600/40" />
            Nenhum documento enviado ainda.
          </p>
        )}

        <ul className={`flex flex-col divide-y divide-cream-200 transition-opacity ${isPlaceholderData ? "opacity-50" : ""}`}>
          {documentsPage?.items.map((doc) => (
            <li key={doc.id} className="flex items-center justify-between gap-3 py-3">
              <div>
                <p className="text-sm font-medium text-navy-900">{doc.fileName}</p>
                <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1">
                  <Badge tone="neutral">{documentCategoryLabels[doc.category]}</Badge>
                  <span className="text-xs text-navy-700/50">
                    {formatFileSize(doc.sizeBytes)} · {formatDateTime(doc.createdAt)}
                  </span>
                </div>
              </div>
              <div className="flex shrink-0 gap-1">
                <Button
                  variant="ghost"
                  onClick={() => handleDownload(doc.id, doc.fileName)}
                  isLoading={downloadingId === doc.id}
                >
                  Baixar
                </Button>
                {canEdit && (
                  <Button variant="ghostDanger" onClick={() => deleteDocument.mutate(doc.id)} disabled={deleteDocument.isPending}>
                    Remover
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>

        {documentsPage && documentsPage.totalCount > 0 && (
          <div className="mt-4 flex items-center justify-between border-t border-cream-200 pt-3 text-sm">
            <span className="text-navy-700/70">
              {documentsPage.totalCount} {documentsPage.totalCount === 1 ? "documento" : "documentos"}
            </span>
            <div className="flex items-center gap-3">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="font-medium text-brand-700 hover:underline disabled:cursor-not-allowed disabled:text-navy-700/30 disabled:hover:no-underline"
              >
                Anterior
              </button>
              <span className="text-navy-700/70">
                Pagina {page} de {totalPages}
              </span>
              <button
                type="button"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="font-medium text-brand-700 hover:underline disabled:cursor-not-allowed disabled:text-navy-700/30 disabled:hover:no-underline"
              >
                Proxima
              </button>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}
