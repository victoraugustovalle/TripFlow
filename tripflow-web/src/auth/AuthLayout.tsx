import type { ReactNode } from "react";
import { FlightTrail } from "../components/FlightTrail";
import { Mascot } from "../components/Mascot";

interface AuthLayoutProps {
  pose: "wave" | "checklist";
  children: ReactNode;
}

export function AuthLayout({ pose, children }: AuthLayoutProps) {
  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center gap-6 overflow-hidden bg-cream-200 px-4 py-10 sm:flex-row sm:gap-10">
      <FlightTrail className="pointer-events-none absolute left-[8%] top-[18%] h-24 w-48 text-brand-600/20 sm:h-32 sm:w-64" />
      <FlightTrail className="pointer-events-none absolute bottom-[12%] right-[10%] h-20 w-40 rotate-180 text-coral-500/20 sm:h-28 sm:w-56" />
      <Mascot pose={pose} className="relative h-28 w-28 shrink-0 sm:h-36 sm:w-36" />
      <div className="relative">{children}</div>
    </div>
  );
}
