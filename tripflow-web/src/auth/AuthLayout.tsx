import type { ReactNode } from "react";
import { FlightTrail } from "../components/FlightTrail";
import { Mascot } from "../components/Mascot";

interface AuthLayoutProps {
  pose: "wave" | "checklist";
  children: ReactNode;
}

export function AuthLayout({ pose, children }: AuthLayoutProps) {
  return (
    <div className="flex min-h-screen flex-col lg:flex-row">
      {/* Faixa compacta pra telas pequenas - o painel completo abaixo ocuparia altura
          demais antes do formulario, que e o que a pessoa realmente veio fazer aqui. */}
      <div className="flex items-center gap-2.5 bg-brand-800 px-6 py-3.5 text-cream-50 lg:hidden">
        <Mascot pose={pose} className="h-9 w-9 shrink-0" />
        <span className="font-display text-base font-semibold">TripFlow</span>
      </div>

      <aside className="relative hidden flex-col justify-between gap-10 overflow-hidden bg-brand-800 px-12 py-12 text-cream-50 lg:flex lg:w-[44%] xl:w-2/5">
        <FlightTrail className="pointer-events-none absolute -left-8 top-20 h-24 w-48 text-cream-100/10" />
        <FlightTrail className="pointer-events-none absolute -right-10 bottom-28 h-20 w-48 rotate-180 text-coral-300/15" />

        <span className="font-display relative text-lg font-semibold tracking-wide">TripFlow</span>

        <div className="relative flex flex-col items-start gap-6">
          <Mascot pose={pose} className="h-32 w-32" />
          <p className="font-display max-w-sm text-2xl leading-snug font-semibold text-cream-50">
            Divida gastos, monte o roteiro e acompanhe tudo com o grupo — em tempo real.
          </p>
          <TripPreviewCard />
        </div>

        <p className="relative text-xs text-cream-100/50">Um lugar so para cada viagem em grupo.</p>
      </aside>

      <div className="flex flex-1 items-center justify-center bg-cream-200 px-4 py-10">{children}</div>
    </div>
  );
}

/** Previa estatica de uma viagem ficticia - mostra o mecanismo do produto (divisao de
 * gastos, checklist, presenca em tempo real) em vez de descreve-lo numa frase generica. */
function TripPreviewCard() {
  return (
    <div className="w-full max-w-xs rounded-xl border border-cream-50/15 bg-white/10 p-4 backdrop-blur-sm">
      <div className="mb-3 flex items-center justify-between">
        <span className="text-sm font-semibold text-cream-50">Praia do Rosa</span>
        <span className="flex items-center gap-1.5 text-xs text-cream-100/70">
          <span className="h-1.5 w-1.5 rounded-full bg-brand-300" aria-hidden="true" />
          3 online agora
        </span>
      </div>

      <div className="mb-3">
        <div className="mb-1 flex justify-between text-xs text-cream-100/70">
          <span>Hospedagem</span>
          <span>R$ 980 de R$ 1.240</span>
        </div>
        <div className="h-1.5 overflow-hidden rounded-full bg-white/15">
          <div className="h-full rounded-full bg-brand-300" style={{ width: "79%" }} />
        </div>
      </div>

      <div className="flex items-center justify-between text-xs text-cream-100/70">
        <span>Checklist</span>
        <span>6 de 8 prontos</span>
      </div>
    </div>
  );
}
