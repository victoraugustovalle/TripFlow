import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { NotificationDto } from "../api/types";
import { Badge } from "../components/Badge";
import { formatDateTime, proactiveNotificationTypes } from "../utils/labels";
import { useMarkAllNotificationsAsRead, useMarkNotificationAsRead, useNotifications } from "./hooks";

function BellIcon() {
  return (
    <svg viewBox="0 0 20 20" className="h-5 w-5" fill="none" aria-hidden="true">
      <path
        d="M10 2.5c-2.3 0-4 1.9-4 4.2v2.1c0 .5-.2 1-.5 1.4l-1.1 1.4c-.5.6-.1 1.6.7 1.6h9.8c.8 0 1.2-1 .7-1.6l-1.1-1.4c-.3-.4-.5-.9-.5-1.4V6.7c0-2.3-1.7-4.2-4-4.2Z"
        stroke="currentColor"
        strokeWidth="1.3"
        strokeLinejoin="round"
      />
      <path d="M8.2 15.8a1.9 1.9 0 0 0 3.6 0" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" />
    </svg>
  );
}

const COMPACT_SIZE = 20;
const EXPANDED_SIZE = 50;

export function NotificationBell() {
  const [expanded, setExpanded] = useState(false);
  const { data } = useNotifications(expanded ? EXPANDED_SIZE : COMPACT_SIZE);
  const markAsRead = useMarkNotificationAsRead();
  const markAllAsRead = useMarkAllNotificationsAsRead();
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) setIsOpen(false);
    };
    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  const unreadCount = data?.unreadCount ?? 0;

  const onSelectNotification = (notification: NotificationDto) => {
    if (!notification.isRead) markAsRead.mutate(notification.id);
    setIsOpen(false);
    navigate(`/trips/${notification.tripId}`);
  };

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((v) => !v)}
        aria-label="Notificacoes"
        className="relative rounded-full p-2 text-navy-700 transition-colors hover:bg-cream-200"
      >
        <BellIcon />
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-coral-600 px-1 text-[10px] font-semibold text-white">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 z-40 mt-2 w-80 rounded-2xl border border-cream-300 bg-white p-2 shadow-xl">
          <div className="flex items-center justify-between px-2 py-1">
            <span className="text-sm font-semibold text-navy-900">Notificacoes</span>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={() => markAllAsRead.mutate()}
                className="text-xs font-medium text-brand-700 hover:underline"
              >
                Marcar todas como lidas
              </button>
            )}
          </div>

          {(!data || data.items.length === 0) && (
            <p className="px-2 py-3 text-sm text-navy-700/70">Nenhuma notificacao ainda.</p>
          )}

          <ul className="flex max-h-96 flex-col divide-y divide-cream-200 overflow-y-auto">
            {data?.items.map((notification) => (
              <li key={notification.id}>
                <button
                  type="button"
                  onClick={() => onSelectNotification(notification)}
                  className={`flex w-full flex-col gap-0.5 rounded-lg px-2 py-2 text-left transition-colors hover:bg-cream-100 ${
                    notification.isRead ? "" : "bg-brand-50/60"
                  }`}
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-xs font-medium text-brand-700">{notification.tripName}</span>
                    {proactiveNotificationTypes.has(notification.type) && (
                      <Badge tone="neutral" className="px-1.5 py-0 text-[10px]">
                        Aviso automatico
                      </Badge>
                    )}
                  </div>
                  <span className="text-sm text-navy-900">{notification.message}</span>
                  <span className="text-xs text-navy-700/50">{formatDateTime(notification.createdAt)}</span>
                </button>
              </li>
            ))}
          </ul>

          {!expanded && data && data.totalCount > COMPACT_SIZE && (
            <button
              type="button"
              onClick={() => setExpanded(true)}
              className="mt-1 w-full rounded-lg px-2 py-2 text-center text-xs font-medium text-brand-700 hover:bg-cream-100 hover:underline"
            >
              Carregar mais
            </button>
          )}
        </div>
      )}
    </div>
  );
}
