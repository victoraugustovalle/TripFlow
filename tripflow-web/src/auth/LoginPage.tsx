import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { z } from "zod";
import * as authApi from "../api/auth";
import { getErrorMessage } from "../api/errors";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Input } from "../components/Input";
import { useAuthStore } from "./authStore";

const schema = z.object({
  email: z.string().email("Informe um e-mail valido."),
  password: z.string().min(1, "Informe a senha."),
});

type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    setFormError(null);
    try {
      const result = await authApi.login(values.email, values.password);

      if (result.requiresTwoFactor) {
        navigate("/2fa/verify", { state: { email: values.email, challengeToken: result.twoFactorChallengeToken } });
        return;
      }

      setSession(result.auth!.accessToken, result.auth!.user);
      navigate("/");
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <Card className="w-full max-w-sm">
        <h1 className="mb-1 text-xl font-semibold text-slate-900">Entrar no TripFlow</h1>
        <p className="mb-6 text-sm text-slate-500">Organize a viagem em grupo sem perder o controle.</p>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <Input label="E-mail" type="email" autoComplete="email" error={errors.email?.message} {...register("email")} />
          <Input label="Senha" type="password" autoComplete="current-password" error={errors.password?.message} {...register("password")} />

          {formError && <Alert message={formError} />}

          <Button type="submit" isLoading={isSubmitting} className="mt-2 w-full">
            Entrar
          </Button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-500">
          Nao tem conta?{" "}
          <Link to="/register" className="font-medium text-brand-600 hover:text-brand-700">
            Criar conta
          </Link>
        </p>
      </Card>
    </div>
  );
}
