using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Journal.DTOs;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Journal;

/// <summary>
/// Atravessa a particao por Trip que domina o resto da API pra responder uma pergunta que
/// nenhum endpoint hoje responde: quais foram as viagens do usuario? Junta todas as viagens
/// concluidas de que ele participou numa linha do tempo pessoal - so compoe dado que ja existe
/// (gasto total via Expense, nota media via TripMemory), igual ao Overview e a Retrospectiva
/// fazem por viagem, sem introduzir regra de negocio nova.
/// </summary>
public class JournalService
{
    private readonly IAppDbContext _db;

    public JournalService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<MyJournalDto> GetMyJournalAsync(Guid userId, CancellationToken ct = default)
    {
        var participations = await _db.TripParticipants.AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == ParticipantStatus.Accepted)
            .Include(p => p.Trip)
            .ToListAsync(ct);

        var completedTrips = participations
            .Select(p => p.Trip)
            .Where(t => t is not null && t.Status == TripStatus.Completed)
            .Select(t => t!)
            .ToList();

        if (completedTrips.Count == 0)
            return new MyJournalDto(0, 0, 0, []);

        var tripIds = completedTrips.Select(t => t.Id).ToList();

        var spentByTrip = await _db.Expenses.AsNoTracking()
            .Where(e => tripIds.Contains(e.TripId))
            .GroupBy(e => e.TripId)
            .Select(g => new { TripId = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.TripId, x => x.Total, ct);

        var ratingsByTrip = await _db.TripMemories.AsNoTracking()
            .Where(m => tripIds.Contains(m.TripId) && m.Rating != null)
            .GroupBy(m => m.TripId)
            .Select(g => new { TripId = g.Key, Average = g.Average(m => (double)m.Rating!.Value) })
            .ToDictionaryAsync(x => x.TripId, x => x.Average, ct);

        var entries = completedTrips
            .OrderByDescending(t => t.EndDate ?? t.StartDate ?? DateOnly.MinValue)
            .Select(t => new JournalEntryDto(
                t.Id,
                t.Name,
                t.Destination,
                t.StartDate,
                t.EndDate,
                t.CoverImageUrl,
                t.Currency,
                spentByTrip.GetValueOrDefault(t.Id, 0m),
                ratingsByTrip.TryGetValue(t.Id, out var average) ? average : null))
            .ToList();

        var destinationsCount = completedTrips
            .Select(t => t.Destination)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var totalDaysTraveled = completedTrips.Sum(t => t.StartDate is { } start && t.EndDate is { } end ? end.DayNumber - start.DayNumber + 1 : 0);

        return new MyJournalDto(completedTrips.Count, destinationsCount, totalDaysTraveled, entries);
    }
}
