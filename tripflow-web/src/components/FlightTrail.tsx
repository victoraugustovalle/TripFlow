/**
 * Trilha pontilhada com aviaozinho de papel na ponta - a assinatura grafica que faltava
 * ao redor do Voo (emprestada do gesto do template TravelToor, redesenhada como elemento
 * proprio). Usa currentColor de proposito: o chamador controla a cor via className de texto
 * (ex. "text-cream-100/50") e a opacidade do contexto, sem precisar de outra prop.
 */
export function FlightTrail({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 200 100" fill="none" aria-hidden="true" className={className}>
      <path
        d="M4 74 Q 46 18 96 46 T 178 18"
        stroke="currentColor"
        strokeWidth="2.5"
        strokeDasharray="1 9"
        strokeLinecap="round"
      />
      <g transform="translate(176 14) rotate(35)">
        <path d="M0 7 L16 0 L0 -7 L4 0 Z" fill="currentColor" />
      </g>
    </svg>
  );
}
