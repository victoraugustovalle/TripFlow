import { Link } from "react-router-dom";

export function BackLink({ to, label }: { to: string; label: string }) {
  return (
    <Link to={to} className="flex w-fit items-center gap-1 text-sm font-medium text-navy-700/70 hover:text-brand-700">
      <span aria-hidden="true">&larr;</span> {label}
    </Link>
  );
}
