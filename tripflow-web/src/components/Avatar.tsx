// Referencia direta aos custom properties do @theme (ver index.css) em vez de hex
// duplicado - assim a paleta do avatar nunca dessincroniza da paleta da marca.
const PALETTE = [
  "var(--color-brand-700)",
  "var(--color-coral-700)",
  "var(--color-amber-700)",
  "var(--color-brand-600)",
  "var(--color-coral-600)",
  "var(--color-brand-800)",
];

function hashString(value: string) {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

export function Avatar({ name, className = "h-8 w-8 text-xs" }: { name: string; className?: string }) {
  const color = PALETTE[hashString(name) % PALETTE.length];

  return (
    <span
      aria-hidden="true"
      className={`inline-flex shrink-0 items-center justify-center rounded-full font-bold text-white ${className}`}
      style={{ backgroundColor: color }}
    >
      {initials(name)}
    </span>
  );
}
