import { type InputHTMLAttributes, forwardRef } from "react";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { label, error, id, className = "", ...props },
  ref,
) {
  const inputId = id ?? props.name;

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={inputId} className="text-sm font-medium text-slate-700">
        {label}
      </label>
      <input
        ref={ref}
        id={inputId}
        className={`rounded-lg border px-3 py-2 text-sm text-slate-900 shadow-sm outline-none transition-colors
          focus:border-brand-500 focus:ring-1 focus:ring-brand-500
          ${error ? "border-red-400" : "border-slate-300"} ${className}`}
        aria-invalid={Boolean(error)}
        {...props}
      />
      {error && <span className="text-sm text-red-600">{error}</span>}
    </div>
  );
});
