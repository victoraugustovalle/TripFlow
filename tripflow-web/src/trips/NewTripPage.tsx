import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import { getErrorMessage } from "../api/errors";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Input } from "../components/Input";
import { useCreateTrip } from "./hooks";

const schema = z.object({
  name: z.string().min(1, "Informe um nome pra viagem."),
  destination: z.string().optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  currency: z.string().length(3, "Use o codigo de 3 letras (ex: BRL)."),
});

type FormValues = z.infer<typeof schema>;

export function NewTripPage() {
  const navigate = useNavigate();
  const createTrip = useCreateTrip();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { currency: "BRL" } });

  const onSubmit = async (values: FormValues) => {
    const trip = await createTrip.mutateAsync({
      name: values.name,
      destination: values.destination || null,
      description: null,
      startDate: values.startDate || null,
      endDate: values.endDate || null,
      currency: values.currency.toUpperCase(),
    });
    navigate(`/trips/${trip.id}`);
  };

  return (
    <div className="mx-auto max-w-md">
      <h1 className="mb-6 text-2xl font-semibold text-slate-900">Nova viagem</h1>

      <Card>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <Input label="Nome" error={errors.name?.message} {...register("name")} />
          <Input label="Destino" error={errors.destination?.message} {...register("destination")} />
          <div className="grid grid-cols-2 gap-4">
            <Input label="Inicio" type="date" error={errors.startDate?.message} {...register("startDate")} />
            <Input label="Fim" type="date" error={errors.endDate?.message} {...register("endDate")} />
          </div>
          <Input label="Moeda" maxLength={3} error={errors.currency?.message} {...register("currency")} />

          {createTrip.isError && <Alert message={getErrorMessage(createTrip.error)} />}

          <Button type="submit" isLoading={isSubmitting} className="mt-2 w-full">
            Criar viagem
          </Button>
        </form>
      </Card>
    </div>
  );
}
