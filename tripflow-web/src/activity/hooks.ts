import { useQuery } from "@tanstack/react-query";
import { listTripActivity } from "../api/activity";

export function useTripActivity(tripId: string, pageSize = 8) {
  return useQuery({
    queryKey: ["activity", tripId, pageSize],
    queryFn: () => listTripActivity(tripId, 1, pageSize),
  });
}
