using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Retrospective.DTOs;
using TripFlow.Domain.Enums;
using TripFlow.Domain.Services;

namespace TripFlow.Application.Retrospective;

/// <summary>
/// Assim como o Overview, so compoe dado que ja existe - nao introduz regra de negocio nova.
/// Pensado pra viagem concluida (o unico motivo real de reabrir o app depois que ela acaba),
/// mas nao trava por status - a API devolve o resumo pra qualquer viagem, quem decide quando
/// mostrar essa tela e o frontend.
/// </summary>
public class RetrospectiveService
{
    private const int MaxTopSpenders = 10;

    private readonly IAppDbContext _db;

    public RetrospectiveService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<TripRetrospectiveDto?> GetAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is null)
            return null;

        var expenses = await _db.Expenses.AsNoTracking().Include(e => e.Splits).Where(e => e.TripId == tripId).ToListAsync(ct);
        var budgets = await _db.Budgets.AsNoTracking().Where(b => b.TripId == tripId).ToListAsync(ct);
        var participants = await _db.TripParticipants.AsNoTracking().Where(p => p.TripId == tripId).ToListAsync(ct);
        var checklistItems = await _db.ChecklistItems.AsNoTracking().Where(c => c.TripId == tripId).ToListAsync(ct);
        var itineraryItemsCount = await _db.ItineraryItems.CountAsync(i => i.TripId == tripId, ct);
        var memories = await _db.TripMemories.AsNoTracking().Where(m => m.TripId == tripId).ToListAsync(ct);

        var balances = SettlementCalculator.CalculateBalances(expenses);
        var topSpenders = balances
            .Where(b => b.TotalPaid > 0)
            .OrderByDescending(b => b.TotalPaid)
            .Take(MaxTopSpenders)
            .Select(b =>
            {
                var participant = participants.FirstOrDefault(p => p.Id == b.ParticipantId);
                var displayName = participant?.DisplayName ?? participant?.InvitedEmail ?? "Participante removido";
                return new TopSpenderDto(b.ParticipantId, displayName, b.TotalPaid);
            })
            .ToList();

        var durationDays = trip.StartDate is { } start && trip.EndDate is { } end
            ? end.DayNumber - start.DayNumber + 1
            : (int?)null;

        var memoryDtos = memories
            .OrderByDescending(m => m.UpdatedAt)
            .Select(m =>
            {
                var participant = participants.FirstOrDefault(p => p.Id == m.ParticipantId);
                var displayName = participant?.DisplayName ?? participant?.InvitedEmail ?? "Participante removido";
                return new TripMemoryDto(m.ParticipantId, displayName, m.Highlight, m.Rating, m.PhotoUrl, m.UpdatedAt);
            })
            .ToList();

        var ratings = memories.Where(m => m.Rating is { } r && r > 0).Select(m => m.Rating!.Value).ToList();
        var averageRating = ratings.Count > 0 ? (double?)ratings.Average() : null;

        return new TripRetrospectiveDto(
            trip.Name,
            trip.StartDate,
            trip.EndDate,
            durationDays,
            expenses.Sum(e => e.Amount),
            budgets.Sum(b => b.PlannedAmount),
            topSpenders,
            checklistItems.Count,
            checklistItems.Count(c => c.IsDone),
            itineraryItemsCount,
            participants.Count(p => p.Status == ParticipantStatus.Accepted),
            memoryDtos,
            averageRating);
    }
}
