import { Link } from "react-router-dom";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Spinner } from "../components/Spinner";
import { tripRoleLabels, tripStatusLabels } from "../utils/labels";
import { useTrips } from "./hooks";

export function TripsListPage() {
  const { data: trips, isLoading, isError } = useTrips();

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-slate-900">Suas viagens</h1>
        <Link to="/trips/new">
          <Button>Nova viagem</Button>
        </Link>
      </div>

      {isLoading && <Spinner />}
      {isError && <p className="text-sm text-red-600">Nao foi possivel carregar suas viagens.</p>}

      {trips && trips.length === 0 && (
        <Card className="text-center text-slate-500">
          Voce ainda nao tem nenhuma viagem. <Link to="/trips/new" className="text-brand-600">Crie a primeira.</Link>
        </Card>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {trips?.map((trip) => (
          <Link key={trip.id} to={`/trips/${trip.id}`}>
            <Card className="h-full transition-shadow hover:shadow-md">
              <div className="mb-2 flex items-start justify-between">
                <h2 className="text-lg font-medium text-slate-900">{trip.name}</h2>
                <span className="rounded-full bg-brand-100 px-2 py-0.5 text-xs font-medium text-brand-700">
                  {tripRoleLabels[trip.myRole]}
                </span>
              </div>
              {trip.destination && <p className="text-sm text-slate-500">{trip.destination}</p>}
              <p className="mt-3 text-xs font-medium uppercase tracking-wide text-slate-400">
                {tripStatusLabels[trip.status]}
              </p>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
