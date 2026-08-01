import { Link } from "react-router-dom";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { FlightTrail } from "../components/FlightTrail";
import { Mascot } from "../components/Mascot";
import { SkeletonCards } from "../components/Skeleton";
import { formatCountdown, formatCurrency, formatDate, tripRoleLabels, tripStatusLabels, tripStatusTone } from "../utils/labels";
import { useMyJournal, useTrips } from "./hooks";

export function TripsListPage() {
  const { data: trips, isLoading, isError } = useTrips();
  // Viagens concluidas vivem no diario logo abaixo - mante-las tambem aqui duplicaria o card.
  const activeTrips = trips?.filter((trip) => trip.status !== 2);

  return (
    <div className="flex flex-col gap-8">
      <div className="flex items-center justify-between">
        <h1 className="font-display text-2xl font-semibold text-navy-900">Suas viagens</h1>
        <Link to="/trips/new">
          <Button>Nova viagem</Button>
        </Link>
      </div>

      <TripJournalSection />

      {isLoading && <SkeletonCards />}
      {isError && (
        <p className="flex items-center gap-2 text-sm text-coral-700">
          <FlightTrail className="h-5 w-8 shrink-0 text-coral-600/40" />
          Nao foi possivel carregar suas viagens. Tente recarregar a pagina.
        </p>
      )}

      {trips && trips.length === 0 && (
        <Card className="flex flex-col items-center gap-3 py-8 text-center text-navy-700/70">
          <Mascot pose="sleeping" className="h-28 w-28" />
          <p>
            Voce ainda nao tem nenhuma viagem. Ela esta cochilando ate a primeira aparecer.
            <br />
            <Link to="/trips/new" className="font-medium text-brand-700 hover:underline">
              Criar a primeira viagem
            </Link>
          </p>
        </Card>
      )}

      {trips && trips.length > 0 && activeTrips?.length === 0 && (
        <Card className="flex flex-col items-center gap-2 py-6 text-center text-navy-700/70">
          <p>Nenhuma viagem em planejamento ou em andamento agora.</p>
          <Link to="/trips/new" className="font-medium text-brand-700 hover:underline">
            Planejar a proxima
          </Link>
        </Card>
      )}

      {activeTrips && activeTrips.length > 0 && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {activeTrips.map((trip) => {
            const dateRange = [formatDate(trip.startDate), formatDate(trip.endDate)].filter(Boolean).join(" – ");
            const countdown = formatCountdown(trip.startDate, trip.status);
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
                    {countdown && (
                      <p className="mt-1.5 text-xs font-semibold text-brand-700">{countdown}</p>
                    )}
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}

/** Cross-trip: a unica secao da home que atravessa viagens em vez de listar uma de cada vez -
 * some por completo (retorna null) enquanto carrega ou quando o usuario ainda nao tem nenhuma
 * viagem concluida, pra nao ocupar espaco com uma promessa vazia antes de existir conteudo real. */
function TripJournalSection() {
  const { data: journal, isLoading } = useMyJournal();

  if (isLoading || !journal || journal.trips.length === 0) return null;

  const stats = [
    `${journal.tripsCompletedCount} ${journal.tripsCompletedCount === 1 ? "viagem concluida" : "viagens concluidas"}`,
    journal.destinationsCount > 0 && `${journal.destinationsCount} ${journal.destinationsCount === 1 ? "destino" : "destinos"}`,
    journal.totalDaysTraveled > 0 && `${journal.totalDaysTraveled} ${journal.totalDaysTraveled === 1 ? "dia viajado" : "dias viajados"}`,
  ]
    .filter(Boolean)
    .join(" · ");

  return (
    <section className="flex flex-col gap-3">
      <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
        <h2 className="font-display text-lg font-semibold text-navy-900">Seu diario de viagens</h2>
        <p className="text-xs text-navy-700/60">{stats}</p>
      </div>

      <div className="flex gap-3 overflow-x-auto pb-1">
        {journal.trips.map((entry) => {
          const dateRange = [formatDate(entry.startDate), formatDate(entry.endDate)].filter((d) => d !== "-").join(" – ");
          return (
            <Link
              key={entry.tripId}
              to={`/trips/${entry.tripId}`}
              className="w-56 shrink-0 overflow-hidden rounded-2xl border border-cream-300 bg-white shadow-sm transition-shadow hover:shadow-md motion-safe:transition-transform motion-safe:hover:-translate-y-0.5"
            >
              <div className="relative h-24 overflow-hidden bg-gradient-to-br from-brand-700 to-navy-900">
                {entry.coverImageUrl ? (
                  <img src={entry.coverImageUrl} alt="" className="absolute inset-0 h-full w-full object-cover" />
                ) : (
                  <FlightTrail className="absolute inset-0 h-full w-full text-cream-100/40" />
                )}
              </div>
              <div className="p-3">
                <h3 className="truncate font-display text-sm font-medium text-navy-900">{entry.name}</h3>
                {entry.destination && <p className="truncate text-xs text-navy-700/70">{entry.destination}</p>}
                <div className="mt-2 flex items-center justify-between text-xs text-navy-700/60">
                  {dateRange && <span>{dateRange}</span>}
                  {entry.averageRating != null && <span className="font-medium text-brand-700">{entry.averageRating.toFixed(1)} / 5</span>}
                </div>
                {entry.totalSpent > 0 && (
                  <p className="mt-1 text-xs text-navy-700/50">{formatCurrency(entry.totalSpent, entry.currency)} em gastos</p>
                )}
              </div>
            </Link>
          );
        })}
      </div>
    </section>
  );
}
