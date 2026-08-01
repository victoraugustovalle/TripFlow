import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
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

const schema = z.object({
  password: z.string().min(1, "Informe a senha."),
  code: z.string().min(1, "Informe o codigo."),
});
type FormValues = z.infer<typeof schema>;

export function TwoFactorDisableModal({ onClose }: { onClose: () => void }) {
  const user = useAuthStore((s) => s.user);
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
      await authApi.disableTwoFactor(values.password, values.code);
      if (user) setSession(getAccessToken()!, { ...user, twoFactorEnabled: false });
      pushToast("Verificacao em duas etapas desativada.");
      onClose();
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  };

  return (
    <Modal title="Desativar verificacao em duas etapas" onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <p className="text-sm text-navy-700/70">Confirme com sua senha e um codigo do app autenticador.</p>
        <Input
          label="Senha"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register("password")}
        />
        <Input label="Codigo de 6 digitos" inputMode="numeric" error={errors.code?.message} {...register("code")} />
        {formError && <Alert message={formError} />}
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" variant="danger" isLoading={isSubmitting}>
            Desativar
          </Button>
        </div>
      </form>
    </Modal>
  );
}
