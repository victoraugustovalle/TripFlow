import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as notificationsApi from "../api/notifications";
import type { NotificationType } from "../api/types";
import { pushToast } from "../toast/toastStore";

/** Nao ha push global (o SignalR aqui e escopado por viagem) - o sino se mantem atualizado
 * por polling. 30s e frequente o bastante pro caso de uso (nao e chat) sem gerar trafego a toa. */
const POLL_INTERVAL_MS = 30_000;

export function useNotifications(pageSize = 20) {
  return useQuery({
    queryKey: ["notifications", pageSize],
    queryFn: () => notificationsApi.listNotifications(1, pageSize),
    refetchInterval: POLL_INTERVAL_MS,
  });
}

export function useMarkNotificationAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: notificationsApi.markNotificationAsRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
}

export function useMarkAllNotificationsAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: notificationsApi.markAllNotificationsAsRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
}

export function useMutedNotificationTypes(tripId: string) {
  return useQuery({
    queryKey: ["notification-preferences", tripId],
    queryFn: () => notificationsApi.getMutedNotificationTypes(tripId),
  });
}

export function useSetMutedNotificationTypes(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (mutedTypes: NotificationType[]) => notificationsApi.setMutedNotificationTypes(tripId, mutedTypes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notification-preferences", tripId] });
      pushToast("Preferencias de notificacao salvas.");
    },
  });
}
