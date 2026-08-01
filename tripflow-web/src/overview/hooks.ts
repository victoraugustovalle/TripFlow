import { useQuery } from "@tanstack/react-query";
import { getTripOverview } from "../api/overview";

export function useTripOverview(tripId: string) {
  return useQuery({ queryKey: ["overview", tripId], queryFn: () => getTripOverview(tripId) });
}
