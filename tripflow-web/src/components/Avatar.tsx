const PALETTE = ["#0B7A70", "#C5401F", "#92600A", "#0E9488", "#F04E2C", "#095F58"];

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
