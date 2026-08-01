import { useEffect, useState } from "react";
import type { NotificationType } from "../api/types";
import { Button } from "../components/Button";
import { Modal } from "../components/Modal";
import { SkeletonLines } from "../components/Skeleton";
import { notificationTypeLabels } from "../utils/labels";
import { useMutedNotificationTypes, useSetMutedNotificationTypes } from "./hooks";

const ALL_TYPES = Object.keys(notificationTypeLabels).map(Number) as NotificationType[];

export function NotificationPreferencesModal({ tripId, onClose }: { tripId: string; onClose: () => void }) {
  const { data: mutedTypes, isLoading } = useMutedNotificationTypes(tripId);
  const setMutedTypes = useSetMutedNotificationTypes(tripId);
  const [draft, setDraft] = useState<Set<NotificationType>>(new Set());

  useEffect(() => {
    if (mutedTypes) setDraft(new Set(mutedTypes));
  }, [mutedTypes]);

  const toggle = (type: NotificationType) => {
    setDraft((prev) => {
      const next = new Set(prev);
      if (next.has(type)) next.delete(type);
      else next.add(type);
      return next;
    });
  };

  const handleSave = async () => {
    await setMutedTypes.mutateAsync(Array.from(draft));
    onClose();
  };

  return (
    <Modal title="Preferencias de notificacao" onClose={onClose}>
      <p className="mb-4 text-sm text-navy-700/70">
        Desmarque o que voce nao quer mais ser notificado nessa viagem. Isso nao afeta outros participantes.
      </p>

      {isLoading ? (
        <SkeletonLines />
      ) : (
        <ul className="flex flex-col gap-2">
          {ALL_TYPES.map((type) => (
            <li key={type}>
              <label className="flex items-center gap-3 text-sm text-navy-900">
                <input
                  type="checkbox"
                  checked={!draft.has(type)}
                  onChange={() => toggle(type)}
                  className="h-4 w-4 rounded border-cream-300 text-brand-600 focus:ring-brand-500"
                />
                {notificationTypeLabels[type]}
              </label>
            </li>
          ))}
        </ul>
      )}

      <div className="mt-5 flex justify-end gap-3">
        <Button type="button" variant="secondary" onClick={onClose}>
          Cancelar
        </Button>
        <Button type="button" isLoading={setMutedTypes.isPending} onClick={handleSave}>
          Salvar
        </Button>
      </div>
    </Modal>
  );
}
