import { useEffect } from "react";
import type { ReactNode } from "react";

const sizeClasses = {
  md: "max-w-3xl",
  xl: "max-w-[96vw]",
} as const;

export function Modal({
  title,
  onClose,
  children,
  size = "md",
}: {
  title: string;
  onClose: () => void;
  children: ReactNode;
  size?: keyof typeof sizeClasses;
}) {
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-navy-900/50 p-4" onClick={onClose}>
      <div
        className={`w-full ${sizeClasses[size]} rounded-2xl bg-white p-4 shadow-xl`}
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="mb-3 flex items-center justify-between">
          <h3 className="font-display text-lg font-semibold text-navy-900">{title}</h3>
          <button
            type="button"
            onClick={onClose}
            aria-label="Fechar"
            className="rounded-full p-1 text-xl leading-none text-navy-700 hover:bg-cream-200 hover:text-navy-900"
          >
            &times;
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
