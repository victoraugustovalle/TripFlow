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

const schema = z.object({
  name: z.string().min(1, "Informe seu nome."),
  email: z.string().email("Informe um e-mail valido."),
  password: z
    .string()
    .min(12, "Minimo de 12 caracteres.")
    .regex(/[A-Z]/, "Precisa de uma letra maiuscula.")
    .regex(/[a-z]/, "Precisa de uma letra minuscula.")
    .regex(/[0-9]/, "Precisa de um numero.")
    .regex(/[\W_]/, "Precisa de um caractere especial."),
});

type FormValues = z.infer<typeof schema>;

export function RegisterPage() {
  const navigate = useNavigate();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    setFormError(null);
    try {
      await authApi.register(values.name, values.email, values.password);
      navigate("/confirm-email", { state: { email: values.email } });
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <Card className="w-full max-w-sm">
        <h1 className="mb-1 text-xl font-semibold text-slate-900">Criar conta</h1>
        <p className="mb-6 text-sm text-slate-500">Leva menos de um minuto.</p>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <Input label="Nome" autoComplete="name" error={errors.name?.message} {...register("name")} />
          <Input label="E-mail" type="email" autoComplete="email" error={errors.email?.message} {...register("email")} />
          <Input
            label="Senha"
            type="password"
            autoComplete="new-password"
            error={errors.password?.message}
            {...register("password")}
          />

          {formError && <Alert message={formError} />}

          <Button type="submit" isLoading={isSubmitting} className="mt-2 w-full">
            Criar conta
          </Button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-500">
          Ja tem conta?{" "}
          <Link to="/login" className="font-medium text-brand-600 hover:text-brand-700">
            Entrar
          </Link>
        </p>
      </Card>
    </div>
  );
}
