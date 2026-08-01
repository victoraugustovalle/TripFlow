using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Overview.DTOs;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Overview;

/// <summary>
/// Cruza Roteiro + Reservas + Documentos + Checklist + Orcamento + Participantes numa lista
/// pequena de regras explicitas ("a viagem tem pelo menos um item no roteiro?", "reserva de
/// voo internacional tem passaporte anexado?") pra responder uma pergunta que nenhum modulo
/// isolado responde sozinho: "essa viagem esta pronta?". De proposito sem IA/heuristica vaga -
/// cada regra e uma contagem/comparacao simples e auditavel, e cada uma diz exatamente pra
/// onde o usuario deve ir pra resolver.
/// </summary>
public class TripReadinessService
{
    private readonly IAppDbContext _db;

    public TripReadinessService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<TripReadinessDto> GetAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is null)
            return new TripReadinessDto(0, []);

        var itineraryCount = await _db.ItineraryItems.CountAsync(i => i.TripId == tripId, ct);
        var budgetCount = await _db.Budgets.CountAsync(b => b.TripId == tripId, ct);
        var checklistItems = await _db.ChecklistItems.AsNoTracking().Where(c => c.TripId == tripId).ToListAsync(ct);
        var pendingInvitesCount = await _db.TripParticipants
            .CountAsync(p => p.TripId == tripId && p.Status == ParticipantStatus.Invited, ct);

        // "Internacional" e um proxy simples e honesto: reserva de voo com moeda informada e
        // diferente da moeda da viagem. Nao tenta adivinhar por nome de cidade/pais.
        var hasInternationalFlight = await _db.Reservations.AnyAsync(r =>
            r.TripId == tripId && r.Type == ReservationType.Flight && r.Currency != null && r.Currency != trip.Currency, ct);

        var pendingItems = new List<TripReadinessItemDto>();
        var applicableCount = 0;
        var doneCount = 0;

        applicableCount++;
        if (itineraryCount > 0)
            doneCount++;
        else
            pendingItems.Add(new TripReadinessItemDto("itinerary", "Nenhum item no roteiro ainda.", "itinerary"));

        applicableCount++;
        if (budgetCount > 0)
            doneCount++;
        else
            pendingItems.Add(new TripReadinessItemDto("budget", "Orcamento da viagem ainda nao foi definido.", "expenses"));

        applicableCount++;
        if (checklistItems.Count == 0)
            pendingItems.Add(new TripReadinessItemDto("checklist", "Nenhum item na checklist ainda.", "checklist"));
        else if (checklistItems.All(c => c.IsDone))
            doneCount++;
        else
            pendingItems.Add(new TripReadinessItemDto(
                "checklist", $"{checklistItems.Count(c => !c.IsDone)} item(ns) pendente(s) na checklist.", "checklist"));

        applicableCount++;
        if (pendingInvitesCount == 0)
            doneCount++;
        else
            pendingItems.Add(new TripReadinessItemDto(
                "participants", $"{pendingInvitesCount} convite(s) aguardando resposta.", "participants"));

        if (hasInternationalFlight)
        {
            applicableCount++;
            var hasPassportDocument = await _db.Documents
                .AnyAsync(d => d.TripId == tripId && d.Category == DocumentCategory.Passport, ct);

            if (hasPassportDocument)
                doneCount++;
            else
                pendingItems.Add(new TripReadinessItemDto(
                    "passport", "Reserva de voo internacional cadastrada, mas nenhum passaporte anexado.", "documents"));
        }

        var percentReady = applicableCount == 0 ? 100 : (int)Math.Round(doneCount * 100.0 / applicableCount);

        return new TripReadinessDto(percentReady, pendingItems);
    }
}
