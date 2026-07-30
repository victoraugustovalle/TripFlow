import { Link } from "react-router-dom";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { SkeletonCards } from "../components/Skeleton";
import { formatDate, tripRoleLabels, tripStatusLabels, tripStatusTone } from "../utils/labels";
import { useTrips } from "./hooks";

export function TripsListPage() {
  const { data: trips, isLoading, isError } = useTrips();

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="font-display text-2xl font-semibold text-navy-900">Suas viagens</h1>
        <Link to="/trips/new">
          <Button>Nova viagem</Button>
        </Link>
      </div>

      {isLoading && <SkeletonCards />}
      {isError && <p className="text-sm text-coral-700">Nao foi possivel carregar suas viagens.</p>}

      {trips && trips.length === 0 && (
        <Card className="relative overflow-hidden text-center text-navy-700/70">
          <FlightTrail className="pointer-events-none absolute -right-6 -top-6 h-24 w-48 text-brand-600/15" />
          <p className="relative">
            Voce ainda nao tem nenhuma viagem. <Link to="/trips/new" className="text-brand-700">Crie a primeira.</Link>
          </p>
        </Card>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {trips?.map((trip) => {
          const dateRange = [formatDate(trip.startDate), formatDate(trip.endDate)].filter(Boolean).join(" – ");
          return (
            <Link key={trip.id} to={`/trips/${trip.id}`}>
              <div className="h-full overflow-hidden rounded-2xl border border-cream-300 bg-white shadow-sm transition-shadow hover:shadow-md motion-safe:transition-transform motion-safe:hover:-translate-y-0.5">
                <div className="relative h-20 overflow-hidden bg-gradient-to-br from-brand-500 to-brand-700">
                  {trip.coverImageUrl ? (
                    <img src={trip.coverImageUrl} alt="" className="absolute inset-0 h-full w-full object-cover" />
                  ) : (
                    <FlightTrail className="absolute inset-0 h-full w-full text-cream-100/50" />
                  )}
                  <span className="absolute right-3 top-3 rounded-full bg-white/90 px-2 py-0.5 text-xs font-medium text-brand-700 shadow-sm">
                    {tripRoleLabels[trip.myRole]}
                  </span>
                </div>
                <div className="p-4">
                  <h2 className="font-display text-lg font-medium text-navy-900">{trip.name}</h2>
                  {trip.destination && <p className="text-sm text-navy-700/70">{trip.destination}</p>}
                  <div className="mt-3 flex items-center justify-between">
                    <Badge tone={tripStatusTone[trip.status]}>{tripStatusLabels[trip.status]}</Badge>
                    {dateRange && <p className="text-xs text-navy-700/50">{dateRange}</p>}
                  </div>
                </div>
              </div>
            </Link>
          );
        })}
      </div>
    </div>
  );
}
