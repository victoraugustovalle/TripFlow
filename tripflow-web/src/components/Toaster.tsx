import { useEffect, useState } from "react";
import { useToastStore } from "../toast/toastStore";

const DURATION_MS = 3000;
const EXIT_MS = 200;

function ToastItem({ id, message, variant }: { id: number; message: string; variant: "success" | "error" | "info" }) {
  const dismiss = useToastStore((s) => s.dismiss);
  const [leaving, setLeaving] = useState(false);

  useEffect(() => {
    const leaveTimeout = setTimeout(() => setLeaving(true), DURATION_MS);
    return () => clearTimeout(leaveTimeout);
  }, []);

  useEffect(() => {
    if (!leaving) return;
    const removeTimeout = setTimeout(() => dismiss(id), EXIT_MS);
    return () => clearTimeout(removeTimeout);
  }, [leaving, id, dismiss]);

  return (
    <div
      role="status"
      className={`${leaving ? "toast-leave" : "toast-enter"} pointer-events-auto flex items-center gap-2 rounded-2xl px-4 py-3 text-sm font-medium text-white shadow-xl ${
        variant === "success" ? "bg-brand-800" : variant === "info" ? "bg-navy-900" : "bg-coral-800"
      }`}
    >
      {message}
    </div>
  );
}

export function Toaster() {
  const toasts = useToastStore((s) => s.toasts);

  if (toasts.length === 0) return null;

  return (
    <div className="pointer-events-none fixed inset-x-0 bottom-6 z-[60] flex flex-col items-center gap-2 px-4">
      {toasts.map((t) => (
        <ToastItem key={t.id} id={t.id} message={t.message} variant={t.variant} />
      ))}
    </div>
  );
}
