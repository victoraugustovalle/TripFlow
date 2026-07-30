import type { HTMLAttributes } from "react";

export type BadgeTone = "success" | "warning" | "danger" | "neutral";

const toneClasses: Record<BadgeTone, string> = {
  success: "bg-brand-50 text-brand-700 border-brand-200",
  warning: "bg-amber-50 text-amber-700 border-amber-200",
  danger: "bg-coral-50 text-coral-700 border-coral-200",
  neutral: "bg-cream-200 text-navy-900 border-cream-300",
};

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: BadgeTone;
}

export function Badge({ tone = "neutral", className = "", ...props }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap ${toneClasses[tone]} ${className}`}
      {...props}
    />
  );
}
