import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useLocation, useNavigate } from "react-router-dom";
import { z } from "zod";
import * as authApi from "../api/auth";
import { getErrorMessage } from "../api/errors";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Input } from "../components/Input";
import { useAuthStore } from "./authStore";

const schema = z.object({
  code: z.string().min(1, "Informe o codigo."),
});

type FormValues = z.infer<typeof schema>;

export function TwoFactorVerifyPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const setSession = useAuthStore((s) => s.setSession);

  const state = location.state as { email?: string; challengeToken?: string } | null;
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  if (!state?.email || !state?.challengeToken) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
        <Card className="w-full max-w-sm text-center">
          <p className="text-sm text-slate-600">Sessao de login expirada.</p>
          <Button onClick={() => navigate("/login")} className="mt-4">
            Voltar pro login
          </Button>
        </Card>
      </div>
    );
  }

  const onSubmit = async (values: FormValues) => {
    setFormError(null);
    try {
      const auth = await authApi.verifyTwoFactor(
        state.email!,
        state.challengeToken!,
        useRecoveryCode ? undefined : values.code,
        useRecoveryCode ? values.code : undefined,
      );
      setSession(auth.accessToken, auth.user);
      navigate("/");
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <Card className="w-full max-w-sm">
        <h1 className="mb-1 text-xl font-semibold text-slate-900">Verificacao em duas etapas</h1>
        <p className="mb-6 text-sm text-slate-500">
          {useRecoveryCode ? "Digite um dos seus codigos de recuperacao." : "Digite o codigo do seu app autenticador."}
        </p>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <Input
            label={useRecoveryCode ? "Codigo de recuperacao" : "Codigo de 6 digitos"}
            inputMode={useRecoveryCode ? "text" : "numeric"}
            autoFocus
            error={errors.code?.message}
            {...register("code")}
          />

          {formError && <Alert message={formError} />}

          <Button type="submit" isLoading={isSubmitting} className="w-full">
            Verificar
          </Button>

          <button
            type="button"
            onClick={() => setUseRecoveryCode((v) => !v)}
            className="text-sm text-brand-600 hover:text-brand-700"
          >
            {useRecoveryCode ? "Usar codigo do app autenticador" : "Perdi o acesso ao app - usar codigo de recuperacao"}
          </button>
        </form>
      </Card>
    </div>
  );
}
