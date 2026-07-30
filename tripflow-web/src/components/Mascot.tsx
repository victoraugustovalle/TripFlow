interface MascotProps {
  pose: "wave" | "checklist";
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

export function Mascot({ pose, className }: MascotProps) {
  return (
    <svg viewBox="0 0 160 190" aria-hidden="true" className={className}>
      {pose === "wave" ? (
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
      ) : (
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
      )}
    </svg>
  );
}
