import { useState } from "react";
import { useAuthStore } from "../auth/authStore";
import { BackLink } from "../components/BackLink";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { TwoFactorDisableModal } from "./TwoFactorDisableModal";
import { TwoFactorSetupModal } from "./TwoFactorSetupModal";

export function ProfilePage() {
  const user = useAuthStore((s) => s.user);
  const [modal, setModal] = useState<"enable" | "disable" | null>(null);

  if (!user) return null;

  return (
    <div className="flex flex-col gap-6">
      <BackLink to="/" label="Voltar pras viagens" />

      <Card>
        <h1 className="font-display mb-4 text-xl font-semibold text-navy-900">Sua conta</h1>
        <dl className="flex flex-col gap-3 text-sm">
          <div className="flex items-center justify-between">
            <dt className="text-navy-700/70">Nome</dt>
            <dd className="font-medium text-navy-900">{user.name}</dd>
          </div>
          <div className="flex items-center justify-between">
            <dt className="text-navy-700/70">E-mail</dt>
            <dd className="font-medium text-navy-900">{user.email}</dd>
          </div>
          <div className="flex items-center justify-between">
            <dt className="text-navy-700/70">E-mail confirmado</dt>
            <dd>
              <Badge tone={user.emailConfirmed ? "success" : "warning"}>{user.emailConfirmed ? "Sim" : "Nao"}</Badge>
            </dd>
          </div>
        </dl>
      </Card>

      <Card>
        <h2 className="font-display mb-4 text-lg font-medium text-navy-900">Seguranca</h2>
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-sm font-medium text-navy-900">Verificacao em duas etapas (2FA)</p>
            <p className="mt-1 text-sm text-navy-700/70">
              {user.twoFactorEnabled
                ? "Ativada - o login pede um codigo do app autenticador, alem da senha."
                : "Desativada - adicione uma camada extra de seguranca no login."}
            </p>
          </div>
          <Badge tone={user.twoFactorEnabled ? "success" : "neutral"}>{user.twoFactorEnabled ? "Ativado" : "Desativado"}</Badge>
        </div>
        <div className="mt-4">
          {user.twoFactorEnabled ? (
            <Button variant="danger" onClick={() => setModal("disable")}>
              Desativar 2FA
            </Button>
          ) : (
            <Button onClick={() => setModal("enable")}>Ativar 2FA</Button>
          )}
        </div>
      </Card>

      {modal === "enable" && <TwoFactorSetupModal onClose={() => setModal(null)} />}
      {modal === "disable" && <TwoFactorDisableModal onClose={() => setModal(null)} />}
    </div>
  );
}
