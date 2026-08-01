import { useState } from "react";
import { getErrorMessage } from "../api/errors";
import type { ItineraryItemType } from "../api/types";
import { Alert } from "../components/Alert";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { Modal } from "../components/Modal";
import { itineraryItemTypeLabels } from "../utils/labels";
import { useCreateItineraryProposal } from "./hooks";

interface OptionDraft {
  title: string;
  location: string;
}

const MIN_OPTIONS = 2;
const MAX_OPTIONS = 5;
const MEAL_TYPE = 3 as ItineraryItemType;

/** Campos identicos ao form de item normal (titulo/tipo/data/horario), mas em vez de local +
 * coordenada unicos, uma lista de 2 a 5 opcoes concorrentes - o grupo vota antes de decidir,
 * entao coordenada por opcao fica pra uma proxima versao (nao vale a complexidade de repetir
 * o autocomplete de endereco por opcao logo de cara). */
export function ItineraryProposalFormModal({ tripId, onClose }: { tripId: string; onClose: () => void }) {
  const createProposal = useCreateItineraryProposal(tripId);

  const [title, setTitle] = useState("");
  const [type, setType] = useState<ItineraryItemType>(MEAL_TYPE);
  const [itemDate, setItemDate] = useState(new Date().toISOString().slice(0, 10));
  const [startTime, setStartTime] = useState("");
  const [endTime, setEndTime] = useState("");
  const [options, setOptions] = useState<OptionDraft[]>([
    { title: "", location: "" },
    { title: "", location: "" },
  ]);
  const [formError, setFormError] = useState<string | null>(null);

  const updateOption = (index: number, patch: Partial<OptionDraft>) => {
    setOptions((prev) => prev.map((option, i) => (i === index ? { ...option, ...patch } : option)));
  };

  const addOption = () => setOptions((prev) => (prev.length >= MAX_OPTIONS ? prev : [...prev, { title: "", location: "" }]));
  const removeOption = (index: number) => setOptions((prev) => (prev.length <= MIN_OPTIONS ? prev : prev.filter((_, i) => i !== index)));

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    const trimmedTitle = title.trim();
    const validOptions = options.map((o) => ({ title: o.title.trim(), location: o.location.trim() })).filter((o) => o.title !== "");

    if (!trimmedTitle) {
      setFormError("Informe um titulo pra proposta.");
      return;
    }
    if (validOptions.length < MIN_OPTIONS) {
      setFormError(`Informe pelo menos ${MIN_OPTIONS} opcoes com titulo.`);
      return;
    }

    try {
      await createProposal.mutateAsync({
        title: trimmedTitle,
        description: null,
        type,
        itemDate,
        startTime: startTime ? `${startTime}:00` : null,
        endTime: endTime ? `${endTime}:00` : null,
        options: validOptions.map((o) => ({
          title: o.title,
          description: null,
          location: o.location || null,
          latitude: null,
          longitude: null,
        })),
      });
      onClose();
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  };

  return (
    <Modal title="Propor opcoes pro roteiro" onClose={onClose}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <p className="text-sm text-navy-700/70">
          Em vez de decidir sozinho, proponha 2 ou mais opcoes pro mesmo horario e deixe o grupo votar.
        </p>

        <Input label="Titulo da proposta" placeholder="Ex: Jantar do dia 3" value={title} onChange={(e) => setTitle(e.target.value)} />

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-navy-900" htmlFor="proposal-type">
              Tipo
            </label>
            <select
              id="proposal-type"
              value={type}
              onChange={(e) => setType(Number(e.target.value) as ItineraryItemType)}
              className="rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                transition-colors focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
            >
              {Object.entries(itineraryItemTypeLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </div>
          <Input label="Data" type="date" value={itemDate} onChange={(e) => setItemDate(e.target.value)} />
          <Input label="Inicio (opcional)" type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} />
          <Input label="Fim (opcional)" type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} />
        </div>

        <div className="flex flex-col gap-2">
          <span className="text-sm font-medium text-navy-900">Opcoes</span>
          {options.map((option, index) => (
            <div key={index} className="flex flex-col gap-2 sm:flex-row">
              <input
                placeholder={`Opcao ${index + 1} (ex: Restaurante A)`}
                value={option.title}
                onChange={(e) => updateOption(index, { title: e.target.value })}
                className="flex-1 rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                  focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
              />
              <input
                placeholder="Local (opcional)"
                value={option.location}
                onChange={(e) => updateOption(index, { location: e.target.value })}
                className="flex-1 rounded-lg border border-cream-300 px-3 py-2 text-sm text-navy-900 shadow-sm outline-none
                  focus:border-brand-500 focus:ring-1 focus:ring-brand-500"
              />
              {options.length > MIN_OPTIONS && (
                <Button type="button" variant="ghostDanger" onClick={() => removeOption(index)}>
                  Remover
                </Button>
              )}
            </div>
          ))}
          {options.length < MAX_OPTIONS && (
            <Button type="button" variant="secondary" onClick={addOption} className="self-start">
              + Adicionar opcao
            </Button>
          )}
        </div>

        {(formError || createProposal.isError) && <Alert message={formError ?? getErrorMessage(createProposal.error)} />}

        <div className="flex gap-3">
          <Button type="submit" isLoading={createProposal.isPending} className="flex-1">
            Criar proposta
          </Button>
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
        </div>
      </form>
    </Modal>
  );
}
