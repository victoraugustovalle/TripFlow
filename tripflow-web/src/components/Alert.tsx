export function Alert({ message, variant = "error" }: { message: string; variant?: "error" | "success" | "info" }) {
  const classes = {
    error: "bg-coral-50 text-coral-700 border-coral-200",
    success: "bg-brand-50 text-brand-700 border-brand-200",
    info: "bg-cream-100 text-navy-900 border-cream-300",
  }[variant];

  return <div className={`rounded-lg border px-3 py-2 text-sm ${classes}`}>{message}</div>;
}
