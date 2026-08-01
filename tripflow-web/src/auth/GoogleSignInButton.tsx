import { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import * as authApi from "../api/auth";
import { useAuthStore } from "./authStore";

// So aparece se o client ID do Google estiver configurado (VITE_GOOGLE_CLIENT_ID) - sem isso
// o componente nao renderiza nada, em vez de mostrar um botao quebrado.
const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;
const SCRIPT_ID = "google-identity-services";

interface GoogleCredentialResponse {
  credential: string;
}

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: { client_id: string; callback: (response: GoogleCredentialResponse) => void }) => void;
          renderButton: (parent: HTMLElement, options: Record<string, unknown>) => void;
        };
      };
    };
  }
}

/** So pra extrair o e-mail e mandar como contexto pro passo de 2FA - o backend e quem de
 * fato valida a assinatura do token do Google, isso aqui nao precisa (nem deveria) validar. */
function decodeGoogleIdTokenEmail(idToken: string): string | undefined {
  try {
    const payload = idToken.split(".")[1];
    const decoded = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/"))) as { email?: string };
    return decoded.email;
  } catch {
    return undefined;
  }
}

export function GoogleSignInButton() {
  const containerRef = useRef<HTMLDivElement>(null);
  const setSession = useAuthStore((s) => s.setSession);
  const navigate = useNavigate();

  useEffect(() => {
    if (!GOOGLE_CLIENT_ID) return;

    const handleCredential = async (response: GoogleCredentialResponse) => {
      const result = await authApi.loginWithGoogle(response.credential);

      if (result.requiresTwoFactor) {
        const email = decodeGoogleIdTokenEmail(response.credential);
        navigate("/2fa/verify", { state: { email, challengeToken: result.twoFactorChallengeToken } });
        return;
      }

      setSession(result.auth!.accessToken, result.auth!.user);
      navigate("/");
    };

    const render = () => {
      if (!window.google || !containerRef.current) return;
      window.google.accounts.id.initialize({ client_id: GOOGLE_CLIENT_ID, callback: handleCredential });
      window.google.accounts.id.renderButton(containerRef.current, { theme: "outline", size: "large", width: 320 });
    };

    if (window.google) {
      render();
      return;
    }

    const existingScript = document.getElementById(SCRIPT_ID);
    if (existingScript) {
      existingScript.addEventListener("load", render);
      return () => existingScript.removeEventListener("load", render);
    }

    const script = document.createElement("script");
    script.id = SCRIPT_ID;
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.onload = render;
    document.head.appendChild(script);
  }, [navigate, setSession]);

  if (!GOOGLE_CLIENT_ID) return null;

  return (
    <div className="flex flex-col items-center gap-3">
      <div className="flex w-full items-center gap-3 text-xs text-navy-700/50">
        <span className="h-px flex-1 bg-cream-300" />
        ou
        <span className="h-px flex-1 bg-cream-300" />
      </div>
      <div ref={containerRef} />
    </div>
  );
}
