export function Spinner({ label = "Carregando..." }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-10 text-brand-700">
      <span className="h-5 w-5 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden />
      <span className="text-sm">{label}</span>
    </div>
  );
}
