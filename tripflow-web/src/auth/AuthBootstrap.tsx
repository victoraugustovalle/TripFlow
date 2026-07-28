import { type ReactNode, useEffect } from "react";
import { refreshAccessToken } from "../api/client";
import { Spinner } from "../components/Spinner";
import { useAuthStore } from "./authStore";

/**
 * O access token so vive em memoria (nao em localStorage, pra reduzir superficie de XSS) -
 * entao some a cada F5. No boot do app, tenta trocar o cookie httpOnly de refresh por um
 * access token novo antes de decidir se mostra a tela de login ou o app autenticado.
 *
 * Usa refreshAccessToken() (deduplicado), nao uma chamada direta - o StrictMode do React
 * monta esse componente duas vezes em dev, e duas chamadas de refresh concorrentes fariam
 * a segunda ser tratada como reuso de token, derrubando a sessao que a primeira acabou de criar.
 */
export function AuthBootstrap({ children }: { children: ReactNode }) {
  const status = useAuthStore((s) => s.status);

  useEffect(() => {
    void refreshAccessToken();
  }, []);

  if (status === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner label="Carregando sessao..." />
      </div>
    );
  }

  return <>{children}</>;
}
