import { type ButtonHTMLAttributes, forwardRef } from "react";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger" | "ghost" | "ghostDanger";
  isLoading?: boolean;
}

const variantClasses: Record<NonNullable<ButtonProps["variant"]>, string> = {
  primary: "bg-brand-700 text-white shadow-sm hover:bg-brand-800 focus-visible:outline-brand-600",
  secondary:
    "bg-white text-brand-700 border border-cream-300 hover:bg-cream-100 focus-visible:outline-brand-400",
  danger: "bg-coral-700 text-white shadow-sm hover:bg-coral-800 focus-visible:outline-coral-600",
  ghost: "bg-transparent text-navy-700 hover:bg-cream-200 focus-visible:outline-brand-400",
  /** Mesmo peso visual do ghost em repouso, mas avisa a gravidade da acao (remover/excluir) no hover. */
  ghostDanger: "bg-transparent text-navy-700 hover:bg-coral-50 hover:text-coral-700 focus-visible:outline-coral-400",
};

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { variant = "primary", isLoading, disabled, className = "", children, ...props },
  ref,
) {
  return (
    <button
      ref={ref}
      disabled={disabled || isLoading}
      className={`font-display inline-flex min-h-11 items-center justify-center gap-2 rounded-full px-4 py-2 text-sm font-semibold
        transition-colors motion-safe:active:scale-[0.97]
        focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2
        disabled:cursor-not-allowed disabled:opacity-60 ${variantClasses[variant]} ${className}`}
      {...props}
    >
      {isLoading && (
        <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden />
      )}
      {children}
    </button>
  );
});
