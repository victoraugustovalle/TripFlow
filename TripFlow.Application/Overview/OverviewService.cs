using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Overview.DTOs;
using TripFlow.Domain.Enums;
using TripFlow.Domain.Services;

namespace TripFlow.Application.Overview;

/// <summary>
/// Nao introduz regra de negocio nova - so compoe, num unico lugar, dados que ja existem
/// espalhados entre Budgets/Expenses/Checklist/Itinerary/Participants. O objetivo e o
/// frontend conseguir montar a visao geral da viagem com uma chamada em vez de cinco.
/// </summary>
public class OverviewService
{
    private const int UpcomingItemsCount = 5;

    private readonly IAppDbContext _db;
    private readonly TripReadinessService _readinessService;

    public OverviewService(IAppDbContext db, TripReadinessService readinessService)
    {
        _db = db;
        _readinessService = readinessService;
    }

    public async Task<TripOverviewDto> GetAsync(Guid tripId, Guid currentUserId, CancellationToken ct = default)
    {
        var budgets = await _db.Budgets.AsNoTracking().Where(b => b.TripId == tripId).ToListAsync(ct);

        var expenses = await _db.Expenses.AsNoTracking()
            .Include(e => e.Splits)
            .Where(e => e.TripId == tripId)
            .ToListAsync(ct);

        var checklistItems = await _db.ChecklistItems.AsNoTracking().Where(c => c.TripId == tripId).ToListAsync(ct);

        var settlementRecords = await _db.SettlementRecords.AsNoTracking().Where(s => s.TripId == tripId).ToListAsync(ct);
        var confirmedSettlements = settlementRecords.Where(s => s.Status == SettlementRecordStatus.Confirmed).ToList();
        var pendingSettlements = settlementRecords.Where(s => s.Status == SettlementRecordStatus.Pending).ToList();

        var myParticipant = await _db.TripParticipants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TripId == tripId && p.UserId == currentUserId, ct);

        var pendingInvitesCount = await _db.TripParticipants
            .CountAsync(p => p.TripId == tripId && p.Status == ParticipantStatus.Invited, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcomingItems = await _db.ItineraryItems.AsNoTracking()
            .Where(i => i.TripId == tripId && i.ItemDate >= today)
            .OrderBy(i => i.ItemDate).ThenBy(i => i.StartTime)
            .Take(UpcomingItemsCount)
            .Select(i => new UpcomingItineraryItemDto(i.Id, i.Title, i.Type, i.ItemDate, i.StartTime, i.Location))
            .ToListAsync(ct);

        var readiness = await _readinessService.GetAsync(tripId, ct);

        return new TripOverviewDto(
            pendingInvitesCount,
            BuildBudgetOverview(budgets, expenses.GroupBy(e => e.Category).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount))),
            new ChecklistOverviewDto(checklistItems.Count, checklistItems.Count(c => c.IsDone)),
            BuildSettlementOverview(expenses, confirmedSettlements, pendingSettlements, myParticipant?.Id),
            upcomingItems,
            readiness);
    }

    private static BudgetOverviewDto BuildBudgetOverview(
        IReadOnlyList<Domain.Entities.Budget> budgets, IReadOnlyDictionary<string, decimal> actualByCategory)
    {
        var categories = budgets.Select(b => b.Category)
            .Union(actualByCategory.Keys)
            .Distinct()
            .Select(category => new BudgetCategoryOverviewDto(
                category,
                budgets.FirstOrDefault(b => b.Category == category)?.PlannedAmount ?? 0m,
                actualByCategory.GetValueOrDefault(category, 0m)))
            .ToList();

        return new BudgetOverviewDto(categories.Sum(c => c.PlannedAmount), categories.Sum(c => c.ActualAmount), categories);
    }

    private static SettlementOverviewDto BuildSettlementOverview(
        IReadOnlyList<Domain.Entities.Expense> expenses, IReadOnlyList<Domain.Entities.SettlementRecord> confirmedSettlements,
        IReadOnlyList<Domain.Entities.SettlementRecord> pendingSettlements, Guid? myParticipantId)
    {
        if (myParticipantId is null)
            return new SettlementOverviewDto(0m, [], []);

        var balances = SettlementCalculator.CalculateBalances(expenses, confirmedSettlements);
        var transfers = SettlementCalculator.Simplify(balances);

        var myNet = balances.FirstOrDefault(b => b.ParticipantId == myParticipantId)?.Net ?? 0m;
        var myTransfers = transfers
            .Where(t => t.FromParticipantId == myParticipantId || t.ToParticipantId == myParticipantId)
            .Select(t => new SettlementTransferDto(t.FromParticipantId, t.ToParticipantId, t.Amount))
            .ToList();

        var myPendingSettlements = pendingSettlements
            .Where(s => s.FromParticipantId == myParticipantId || s.ToParticipantId == myParticipantId)
            .Select(s => new SettlementRecordDto(s.Id, s.FromParticipantId, s.ToParticipantId, s.Amount, s.Status, s.CreatedAt, s.ConfirmedAt))
            .ToList();

        return new SettlementOverviewDto(myNet, myTransfers, myPendingSettlements);
    }
}
