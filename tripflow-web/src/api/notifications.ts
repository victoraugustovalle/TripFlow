import { apiFetch } from "./client";
import type { NotificationsPageDto, NotificationType } from "./types";

export function listNotifications(page = 1, pageSize = 20) {
  return apiFetch<NotificationsPageDto>(`/api/notifications?page=${page}&pageSize=${pageSize}`);
}

export function markNotificationAsRead(notificationId: string) {
  return apiFetch<void>(`/api/notifications/${notificationId}/read`, { method: "POST" });
}

export function markAllNotificationsAsRead() {
  return apiFetch<void>("/api/notifications/read-all", { method: "POST" });
}

export function getMutedNotificationTypes(tripId: string) {
  return apiFetch<NotificationType[]>(`/api/trips/${tripId}/notification-preferences`);
}

export function setMutedNotificationTypes(tripId: string, mutedTypes: NotificationType[]) {
  return apiFetch<void>(`/api/trips/${tripId}/notification-preferences`, { method: "PUT", body: { mutedTypes } });
}
