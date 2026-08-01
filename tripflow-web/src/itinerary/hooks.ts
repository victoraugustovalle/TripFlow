import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { searchGeocode } from "../api/geocoding";
import * as itineraryApi from "../api/itinerary";
import type { ItineraryItemInput } from "../api/itinerary";
import { pushToast } from "../toast/toastStore";

export function useItinerary(tripId: string) {
  return useQuery({ queryKey: ["itinerary", tripId], queryFn: () => itineraryApi.listItinerary(tripId) });
}

/** So tem previsao pros proximos ~10 dias (limite do provedor) - dias fora dessa janela ou sem
 * nenhum item com coordenadas simplesmente nao aparecem na resposta. */
export function useItineraryWeather(tripId: string) {
  return useQuery({ queryKey: ["itinerary", tripId, "weather"], queryFn: () => itineraryApi.getItineraryWeather(tripId) });
}

export function useCreateItineraryItem(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ItineraryItemInput) => itineraryApi.createItineraryItem(tripId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["itinerary", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Item adicionado ao roteiro.");
    },
  });
}

export function useUpdateItineraryItem(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ itemId, input }: { itemId: string; input: ItineraryItemInput }) =>
      itineraryApi.updateItineraryItem(tripId, itemId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["itinerary", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Alteracoes salvas.");
    },
  });
}

export function useDeleteItineraryItem(tripId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (itemId: string) => itineraryApi.deleteItineraryItem(tripId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["itinerary", tripId] });
      queryClient.invalidateQueries({ queryKey: ["overview", tripId] });
      pushToast("Item removido do roteiro.");
    },
  });
}

/** Busca sob demanda (botao "Buscar") - o rate limit do geocoding e global (1 req/s pra API inteira via Nominatim), nao da pra sair buscando a cada tecla digitada. */
export function useGeocodeSearch() {
  return useMutation({ mutationFn: (query: string) => searchGeocode(query) });
}
