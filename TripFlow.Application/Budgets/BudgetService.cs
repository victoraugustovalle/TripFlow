using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Budgets.DTOs;
using TripFlow.Application.Common;
using TripFlow.Domain.Entities;

namespace TripFlow.Application.Budgets;

public class BudgetService
{
    private readonly IAppDbContext _db;

    public BudgetService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BudgetDto>> ListAsync(Guid tripId, CancellationToken ct = default)
    {
        var budgets = await _db.Budgets.AsNoTracking().Where(b => b.TripId == tripId).ToListAsync(ct);

        var actualByCategory = await _db.Expenses.AsNoTracking()
            .Where(e => e.TripId == tripId)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.Category, x => x.Total, ct);

        var categories = budgets.Select(b => b.Category)
            .Union(actualByCategory.Keys)
            .Distinct();

        return categories.Select(category =>
        {
            var budget = budgets.FirstOrDefault(b => b.Category == category);
            var actual = actualByCategory.GetValueOrDefault(category, 0m);
            return new BudgetDto(budget?.Id ?? Guid.Empty, category, budget?.PlannedAmount ?? 0m, actual);
        }).ToList();
    }

    public async Task<BudgetDto> UpsertAsync(Guid tripId, UpsertBudgetRequest request, CancellationToken ct = default)
    {
        var category = request.Category.Trim();
        var budget = await _db.Budgets.FirstOrDefaultAsync(b => b.TripId == tripId && b.Category == category, ct);

        if (budget is null)
        {
            budget = new Budget { TripId = tripId, Category = category, PlannedAmount = request.PlannedAmount };
            _db.Budgets.Add(budget);
        }
        else
        {
            budget.PlannedAmount = request.PlannedAmount;
        }

        await _db.SaveChangesAsync(ct);

        var actual = await _db.Expenses.AsNoTracking()
            .Where(e => e.TripId == tripId && e.Category == category)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        return new BudgetDto(budget.Id, budget.Category, budget.PlannedAmount, actual);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid budgetId, CancellationToken ct = default)
    {
        var budget = await _db.Budgets.FirstOrDefaultAsync(b => b.Id == budgetId && b.TripId == tripId, ct);
        if (budget is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Orcamento nao encontrado.");

        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}
