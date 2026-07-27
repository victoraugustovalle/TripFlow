using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;
using TripFlow.Domain.Services;

namespace TripFlow.Application.Expenses;

public class ExpenseService
{
    private readonly IAppDbContext _db;

    public ExpenseService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<ExpenseDto>> CreateAsync(Guid tripId, CreateExpenseRequest request, CancellationToken ct = default)
    {
        var payer = await _db.TripParticipants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaidByParticipantId && p.TripId == tripId, ct);
        if (payer is null)
            return ServiceResult<ExpenseDto>.Failure(ServiceErrorType.Validation, "Quem pagou precisa ser um participante da viagem.");

        var splitParticipantIds = request.SplitBetweenParticipantIds is { Count: > 0 }
            ? request.SplitBetweenParticipantIds
            : await _db.TripParticipants.AsNoTracking()
                .Where(p => p.TripId == tripId && p.Status == ParticipantStatus.Accepted)
                .Select(p => p.Id)
                .ToListAsync(ct);

        if (splitParticipantIds.Count == 0)
            return ServiceResult<ExpenseDto>.Failure(ServiceErrorType.Validation, "A despesa precisa ser dividida com pelo menos um participante.");

        var validCount = await _db.TripParticipants.CountAsync(p => p.TripId == tripId && splitParticipantIds.Contains(p.Id), ct);
        if (validCount != splitParticipantIds.Count)
            return ServiceResult<ExpenseDto>.Failure(ServiceErrorType.Validation, "Um ou mais participantes da divisao nao pertencem a essa viagem.");

        var expense = new Expense
        {
            TripId = tripId,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Geral" : request.Category.Trim(),
            PaidByParticipantId = request.PaidByParticipantId,
            ExpenseDate = request.ExpenseDate
        };

        foreach (var (participantId, share) in SplitEqually(request.Amount, splitParticipantIds))
            expense.Splits.Add(new ExpenseSplit { ExpenseId = expense.Id, ParticipantId = participantId, ShareAmount = share });

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<ExpenseDto>.Success(ToDto(expense));
    }

    public async Task<IReadOnlyList<ExpenseDto>> ListAsync(Guid tripId, CancellationToken ct = default)
    {
        var expenses = await _db.Expenses.AsNoTracking()
            .Include(e => e.Splits)
            .Where(e => e.TripId == tripId)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync(ct);

        return expenses.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid expenseId, CancellationToken ct = default)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId && e.TripId == tripId, ct);
        if (expense is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Despesa nao encontrada.");

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<SettlementDto> GetSettlementAsync(Guid tripId, CancellationToken ct = default)
    {
        var expenses = await _db.Expenses.AsNoTracking()
            .Include(e => e.Splits)
            .Where(e => e.TripId == tripId)
            .ToListAsync(ct);

        var balances = SettlementCalculator.CalculateBalances(expenses);
        var transfers = SettlementCalculator.Simplify(balances);

        return new SettlementDto(
            balances.Select(b => new ParticipantBalanceDto(b.ParticipantId, b.TotalPaid, b.TotalOwed, b.Net)).ToList(),
            transfers.Select(t => new SettlementTransferDto(t.FromParticipantId, t.ToParticipantId, t.Amount)).ToList());
    }

    /// <summary>Divide em centavos pra nao perder/sobrar 1 centavo por arredondamento -
    /// o resto vai pros primeiros participantes da lista, um centavo a mais cada.</summary>
    private static IEnumerable<(Guid ParticipantId, decimal Share)> SplitEqually(decimal amount, IReadOnlyList<Guid> participantIds)
    {
        var totalCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        var baseShareCents = totalCents / participantIds.Count;
        var remainderCents = totalCents % participantIds.Count;

        for (var i = 0; i < participantIds.Count; i++)
        {
            var cents = baseShareCents + (i < remainderCents ? 1 : 0);
            yield return (participantIds[i], cents / 100m);
        }
    }

    private static ExpenseDto ToDto(Expense expense) => new(
        expense.Id, expense.Description, expense.Amount, expense.Category, expense.PaidByParticipantId,
        expense.ExpenseDate, expense.CreatedAt,
        expense.Splits.Select(s => new ExpenseSplitDto(s.ParticipantId, s.ShareAmount)).ToList());
}
