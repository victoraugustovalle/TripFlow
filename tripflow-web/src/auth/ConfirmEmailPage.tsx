import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useLocation, useNavigate } from "react-router-dom";
import { z } from "zod";
import * as authApi from "../api/auth";
import { getErrorMessage } from "../api/errors";
import { Alert } from "../components/Alert";
import { BackLink } from "../components/BackLink";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Input } from "../components/Input";
import { AuthLayout } from "./AuthLayout";

const schema = z.object({
  code: z.string().length(6, "O codigo tem 6 digitos."),
});

type FormValues = z.infer<typeof schema>;

export function ConfirmEmailPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const email = (location.state as { email?: string } | null)?.email ?? "";
  const [formError, setFormError] = useState<string | null>(null);
  const [confirmed, setConfirmed] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    setFormError(null);
    try {
      await authApi.confirmEmail(email, values.code);
      setConfirmed(true);
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  };

  return (
    <AuthLayout pose="checklist">
      <Card className="w-full max-w-sm">
        <BackLink to="/login" label="Voltar pro login" />
        <h1 className="font-display mb-1 mt-3 text-xl font-semibold text-navy-900">Confirme seu e-mail</h1>
        <p className="mb-6 text-sm text-navy-700/70">
          Mandamos um codigo de 6 digitos para <strong>{email || "o seu e-mail"}</strong>. Sem SMTP configurado no
          backend, o codigo aparece no log do servidor em vez de chegar por e-mail de verdade.
        </p>

        {confirmed ? (
          <div className="flex flex-col gap-4">
            <Alert variant="success" message="E-mail confirmado! Ja da pra entrar." />
            <Button onClick={() => navigate("/login")} className="w-full">
              Ir para o login
            </Button>
          </div>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <Input label="Codigo" inputMode="numeric" maxLength={6} error={errors.code?.message} {...register("code")} />

            {formError && <Alert message={formError} />}

            <Button type="submit" isLoading={isSubmitting} className="w-full">
              Confirmar
            </Button>
          </form>
        )}
      </Card>
    </AuthLayout>
  );
}
