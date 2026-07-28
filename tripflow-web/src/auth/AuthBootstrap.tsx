import { type ReactNode, useEffect } from "react";
import { doRefresh } from "../api/client";
import { Spinner } from "../components/Spinner";
import { useAuthStore } from "./authStore";

/**
 * O access token so vive em memoria (nao em localStorage, pra reduzir superficie de XSS) -
 * entao some a cada F5. No boot do app, tenta trocar o cookie httpOnly de refresh por um
 * access token novo antes de decidir se mostra a tela de login ou o app autenticado.
 */
export function AuthBootstrap({ children }: { children: ReactNode }) {
  const status = useAuthStore((s) => s.status);

  useEffect(() => {
    // doRefresh ja chama setSession/clearSession internamente conforme o resultado.
    void doRefresh();
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
