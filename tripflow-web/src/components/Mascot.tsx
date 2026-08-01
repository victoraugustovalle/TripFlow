import type { ReactElement } from "react";

interface MascotProps {
  pose: "wave" | "checklist" | "celebrating" | "sleeping";
  className?: string;
}

/** Mao "luva" rubber-hose: circulo maior que o corpo do traço, cor propria (nunca a cor do membro) e uma costura curva - sem isso ele le como "ponta de cano", nao como personagem vintage. */
function Hand({ cx, cy }: { cx: number; cy: number }) {
  return (
    <g>
      <circle cx={cx} cy={cy} r="10" fill="#FFF8EC" stroke="#1B2340" strokeWidth="2.4" />
      <path
        d={`M${cx - 5} ${cy - 1} Q${cx} ${cy + 3.5} ${cx + 5} ${cy - 1}`}
        stroke="#1B2340"
        strokeWidth="1.6"
        fill="none"
        strokeLinecap="round"
      />
    </g>
  );
}

function WavePose() {
  return (
    <>
      <path d="M62 138 Q58 158 63 172" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <ellipse cx="64" cy="175" rx="11" ry="7" fill="#FF6B4A" stroke="#1B2340" strokeWidth="2" />
      <path d="M84 138 Q90 158 84 172" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <ellipse cx="84" cy="175" rx="11" ry="7" fill="#FF6B4A" stroke="#1B2340" strokeWidth="2" />
      <rect x="104" y="100" width="24" height="34" rx="8" fill="#FF6B4A" />
      <ellipse cx="76" cy="98" rx="46" ry="51" fill="#17B8A6" />
      <ellipse cx="76" cy="126" rx="17" ry="14" fill="#FFF8EC" />
      <path d="M56 104 Q40 114 44 132" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <Hand cx={45} cy={135} />
      <path d="M96 88 Q118 78 118 56" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <Hand cx={118} cy={52} />
      <circle cx="60" cy="90" r="5.2" fill="#1B2340" />
      <circle cx="86" cy="90" r="5.2" fill="#1B2340" />
      <ellipse cx="54" cy="101" rx="7" ry="4.6" fill="#FF9478" />
      <ellipse cx="92" cy="101" rx="7" ry="4.6" fill="#FF9478" />
      <path d="M62 106 Q73 115 84 106" stroke="#1B2340" strokeWidth="3.6" fill="none" strokeLinecap="round" />
    </>
  );
}

function ChecklistPose() {
  return (
    <>
      <path d="M60 140 Q48 158 58 172" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <ellipse cx="58" cy="175" rx="11" ry="7" fill="#FF6B4A" stroke="#1B2340" strokeWidth="2" />
      <path d="M88 138 Q98 156 90 172" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <ellipse cx="90" cy="175" rx="11" ry="7" fill="#FF6B4A" stroke="#1B2340" strokeWidth="2" />
      <ellipse cx="74" cy="98" rx="46" ry="51" fill="#17B8A6" />
      <ellipse cx="74" cy="126" rx="17" ry="14" fill="#FFF8EC" />
      <path d="M50 104 Q30 108 26 126" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <Hand cx={24} cy={129} />
      <path d="M98 104 Q116 100 122 92" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <Hand cx={125} cy={89} />
      <rect
        x="103"
        y="66"
        width="30"
        height="24"
        rx="4"
        fill="#FFF8EC"
        stroke="#1B2340"
        strokeWidth="2.4"
        transform="rotate(-14 118 78)"
      />
      <path
        d="M108 74l14-3M110 80l14-3"
        stroke="#FF6B4A"
        strokeWidth="2.4"
        strokeLinecap="round"
        transform="rotate(-14 118 78)"
      />
      <circle cx="58" cy="90" r="5.2" fill="#1B2340" />
      <circle cx="84" cy="90" r="5.2" fill="#1B2340" />
      <ellipse cx="52" cy="101" rx="7" ry="4.6" fill="#FF9478" />
      <ellipse cx="90" cy="101" rx="7" ry="4.6" fill="#FF9478" />
      <path d="M60 106 Q71 115 82 106" stroke="#1B2340" strokeWidth="3.6" fill="none" strokeLinecap="round" />
    </>
  );
}

/** Bracos em V pro alto + confete - reservada pra fechamento de viagem (retrospectiva) e
 * marcos de progresso (100% de prontidao), os momentos de maior carga emocional do produto. */
function CelebratingPose() {
  return (
    <>
      <path d="M58 138 Q48 156 54 172" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <ellipse cx="54" cy="175" rx="11" ry="7" fill="#FF6B4A" stroke="#1B2340" strokeWidth="2" />
      <path d="M90 138 Q100 156 94 172" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <ellipse cx="94" cy="175" rx="11" ry="7" fill="#FF6B4A" stroke="#1B2340" strokeWidth="2" />
      <ellipse cx="76" cy="98" rx="46" ry="51" fill="#17B8A6" />
      <ellipse cx="76" cy="126" rx="17" ry="14" fill="#FFF8EC" />
      <path d="M54 104 Q34 80 30 46" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <Hand cx={28} cy={42} />
      <path d="M98 104 Q118 80 122 46" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <Hand cx={124} cy={42} />
      <path d="M54 88 Q60 82 66 88" stroke="#1B2340" strokeWidth="3" fill="none" strokeLinecap="round" />
      <path d="M84 88 Q90 82 96 88" stroke="#1B2340" strokeWidth="3" fill="none" strokeLinecap="round" />
      <ellipse cx="54" cy="101" rx="7" ry="4.6" fill="#FF9478" />
      <ellipse cx="92" cy="101" rx="7" ry="4.6" fill="#FF9478" />
      <path d="M58 106 Q73 120 88 106" stroke="#1B2340" strokeWidth="3.6" fill="none" strokeLinecap="round" />
      <circle cx="18" cy="28" r="2.6" fill="#F5A524" />
      <circle cx="30" cy="14" r="2" fill="#FF8A63" />
      <circle cx="138" cy="26" r="2.6" fill="#FF8A63" />
      <circle cx="128" cy="12" r="2" fill="#F5A524" />
      <circle cx="8" cy="46" r="1.8" fill="#17B8A6" />
      <circle cx="150" cy="44" r="1.8" fill="#17B8A6" />
    </>
  );
}

/** Bracos recolhidos, olhos fechados e um "z" flutuando - reservada pro empty state de
 * "nenhuma viagem ainda", o momento de menor atividade do produto (nao o de menor
 * importancia - e o primeiro contato de quem acabou de chegar). */
function SleepingPose() {
  return (
    <>
      <path d="M66 138 Q62 156 66 170" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <ellipse cx="66" cy="173" rx="11" ry="7" fill="#FF6B4A" stroke="#1B2340" strokeWidth="2" />
      <path d="M86 138 Q90 156 86 170" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <ellipse cx="86" cy="173" rx="11" ry="7" fill="#FF6B4A" stroke="#1B2340" strokeWidth="2" />
      <ellipse cx="76" cy="98" rx="46" ry="51" fill="#17B8A6" />
      <ellipse cx="76" cy="126" rx="17" ry="14" fill="#FFF8EC" />
      <path d="M58 108 Q46 118 50 132" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <Hand cx={50} cy={134} />
      <path d="M94 108 Q106 118 102 132" stroke="#1B2340" strokeWidth="6" fill="none" strokeLinecap="round" />
      <Hand cx={102} cy={134} />
      <path d="M56 89 Q62 93 68 89" stroke="#1B2340" strokeWidth="3" fill="none" strokeLinecap="round" />
      <path d="M84 89 Q90 93 96 89" stroke="#1B2340" strokeWidth="3" fill="none" strokeLinecap="round" />
      <ellipse cx="54" cy="101" rx="7" ry="4.6" fill="#FF9478" />
      <ellipse cx="92" cy="101" rx="7" ry="4.6" fill="#FF9478" />
      <path d="M64 106 Q73 109 82 106" stroke="#1B2340" strokeWidth="3.6" fill="none" strokeLinecap="round" />
      <text x="110" y="60" fontSize="17" fontWeight="700" fill="#1B2340" opacity="0.55">
        Z
      </text>
      <text x="124" y="44" fontSize="12" fontWeight="700" fill="#1B2340" opacity="0.4">
        z
      </text>
    </>
  );
}

const POSES: Record<MascotProps["pose"], () => ReactElement> = {
  wave: WavePose,
  checklist: ChecklistPose,
  celebrating: CelebratingPose,
  sleeping: SleepingPose,
};

export function Mascot({ pose, className }: MascotProps) {
  const Pose = POSES[pose];
  return (
    <svg viewBox="0 0 160 190" aria-hidden="true" className={className}>
      <Pose />
    </svg>
  );
}
