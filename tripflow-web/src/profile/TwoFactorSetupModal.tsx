import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import * as authApi from "../api/auth";
import { getErrorMessage } from "../api/errors";
import { getAccessToken, useAuthStore } from "../auth/authStore";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { Modal } from "../components/Modal";
import { pushToast } from "../toast/toastStore";

const schema = z.object({ code: z.string().min(1, "Informe o codigo.") });
type FormValues = z.infer<typeof schema>;

type Step = "loading" | "scan" | "recovery-codes";

export function TwoFactorSetupModal({ onClose }: { onClose: () => void }) {
  const user = useAuthStore((s) => s.user);
  const setSession = useAuthStore((s) => s.setSession);

  const [step, setStep] = useState<Step>("loading");
  const [setupData, setSetupData] = useState<{ secret: string; otpAuthUri: string; qrCodePngBase64: string } | null>(null);
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  useEffect(() => {
    authApi
      .setupTwoFactor()
      .then((data) => {
        setSetupData(data);
        setStep("scan");
      })
      .catch((error) => setLoadError(getErrorMessage(error)));
  }, []);

  const onSubmit = async (values: FormValues) => {
    setSubmitError(null);
    try {
      const codes = await authApi.enableTwoFactor(values.code);
      setRecoveryCodes(codes);
      setStep("recovery-codes");
      if (user) setSession(getAccessToken()!, { ...user, twoFactorEnabled: true });
    } catch (error) {
      setSubmitError(getErrorMessage(error));
    }
  };

  const handleFinish = () => {
    pushToast("Verificacao em duas etapas ativada.");
    onClose();
  };

  return (
    <Modal title="Ativar verificacao em duas etapas" onClose={onClose}>
      {step === "loading" &&
        (loadError ? <Alert message={loadError} /> : <p className="text-sm text-navy-700/70">Gerando o QR code...</p>)}

      {step === "scan" && setupData && (
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <p className="text-sm text-navy-700/70">
            Escaneie o QR code com o app autenticador (Google Authenticator, Authy, 1Password...) ou digite o codigo abaixo
            manualmente.
          </p>
          <img
            src={`data:image/png;base64,${setupData.qrCodePngBase64}`}
            alt="QR code para configurar o 2FA"
            className="mx-auto h-40 w-40"
          />
          <p className="break-all rounded-lg bg-cream-100 px-3 py-2 text-center font-mono text-xs text-navy-700">
            {setupData.secret}
          </p>
          <Input
            label="Codigo de 6 digitos"
            inputMode="numeric"
            autoFocus
            error={errors.code?.message}
            {...register("code")}
          />
          {submitError && <Alert message={submitError} />}
          <Button type="submit" isLoading={isSubmitting} className="w-full">
            Confirmar e ativar
          </Button>
        </form>
      )}

      {step === "recovery-codes" && (
        <div className="flex flex-col gap-4">
          <Alert
            variant="info"
            message="Guarde esses codigos agora - eles nao aparecem de novo. Cada um serve pra entrar uma unica vez, caso voce perca o acesso ao app autenticador."
          />
          <ul className="grid grid-cols-2 gap-2 rounded-lg bg-cream-100 p-3 font-mono text-sm text-navy-900">
            {recoveryCodes.map((code) => (
              <li key={code}>{code}</li>
            ))}
          </ul>
          <Button onClick={handleFinish} className="w-full">
            Copiei os codigos - concluir
          </Button>
        </div>
      )}
    </Modal>
  );
}
