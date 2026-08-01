using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity;
using TripFlow.Application.Common;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Notifications;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;
using TripFlow.Domain.Services;

namespace TripFlow.Application.Expenses;

public class ExpenseService
{
    private readonly IAppDbContext _db;
    private readonly ITripNotifier _tripNotifier;
    private readonly ActivityService _activityService;
    private readonly NotificationService _notificationService;

    public ExpenseService(IAppDbContext db, ITripNotifier tripNotifier, ActivityService activityService, NotificationService notificationService)
    {
        _db = db;
        _tripNotifier = tripNotifier;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<ExpenseDto>> CreateAsync(Guid tripId, Guid actorUserId, CreateExpenseRequest request, CancellationToken ct = default)
    {
        var payer = await _db.TripParticipants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaidByParticipantId && p.TripId == tripId, ct);
        if (payer is null)
            return ServiceResult<ExpenseDto>.Failure(ServiceErrorType.Validation, "Quem pagou precisa ser um participante da viagem.");

        List<(Guid ParticipantId, decimal Share)> splits;

        if (request.Shares is { Count: > 0 })
        {
            // Divisao customizada: o valor de cada participante ja vem pronto do cliente (a
            // soma bater com Amount ja foi checado no validator) - so falta confirmar que
            // todo mundo citado realmente pertence a essa viagem.
            var shareParticipantIds = request.Shares.Select(s => s.ParticipantId).ToList();
            var validShareCount = await _db.TripParticipants.CountAsync(p => p.TripId == tripId && shareParticipantIds.Contains(p.Id), ct);
            if (validShareCount != shareParticipantIds.Count)
                return ServiceResult<ExpenseDto>.Failure(ServiceErrorType.Validation, "Um ou mais participantes da divisao nao pertencem a essa viagem.");

            splits = request.Shares.Select(s => (s.ParticipantId, s.ShareAmount)).ToList();
        }
        else
        {
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

            splits = SplitEqually(request.Amount, splitParticipantIds).ToList();
        }

        var expense = new Expense
        {
            TripId = tripId,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Geral" : request.Category.Trim(),
            PaidByParticipantId = request.PaidByParticipantId,
            ExpenseDate = request.ExpenseDate
        };

        foreach (var (participantId, share) in splits)
            expense.Splits.Add(new ExpenseSplit { ExpenseId = expense.Id, ParticipantId = participantId, ShareAmount = share });

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);

        var dto = ToDto(expense);
        await _tripNotifier.NotifyExpenseCreatedAsync(tripId, dto, ct);

        var currency = await _db.Trips.AsNoTracking().Where(t => t.Id == tripId).Select(t => t.Currency).FirstOrDefaultAsync(ct) ?? "BRL";
        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        var amountText = expense.Amount.ToString("0.00", CultureInfo.InvariantCulture);

        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ExpenseCreated, nameof(Expense), expense.Id,
            $"{actorName} lancou um gasto de {amountText} {currency} em {expense.Category}.", ct);

        await _notificationService.NotifyTripAsync(tripId, actorUserId, NotificationType.ExpenseCreated,
            $"{actorName} lancou um gasto de {amountText} {currency} em {expense.Category}.", ct);

        await NotifyIfBudgetJustExceededAsync(tripId, actorUserId, expense.Category, expense.Amount, currency, ct);

        return ServiceResult<ExpenseDto>.Success(dto);
    }

    public async Task<PagedResult<ExpenseDto>> ListAsync(Guid tripId, ExpenseListQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var expensesQuery = _db.Expenses.AsNoTracking().Include(e => e.Splits).Where(e => e.TripId == tripId);

        if (!string.IsNullOrWhiteSpace(query.Category))
            expensesQuery = expensesQuery.Where(e => e.Category == query.Category);

        if (query.From is { } from)
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate >= from);

        if (query.To is { } to)
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate <= to);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ToLower() dos dois lados porque Contains() sozinho e case-sensitive tanto no
            // Postgres (LIKE) quanto no SQLite dos testes - sem isso "hotel" nao acha "Hotel Lisboa".
            var term = query.Search.Trim().ToLowerInvariant();
            expensesQuery = expensesQuery.Where(e => e.Description.ToLower().Contains(term) || e.Category.ToLower().Contains(term));
        }

        var totalCount = await expensesQuery.CountAsync(ct);

        var expenses = await expensesQuery
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ExpenseDto>(expenses.Select(ToDto).ToList(), totalCount, page, pageSize);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid actorUserId, Guid expenseId, CancellationToken ct = default)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId && e.TripId == tripId, ct);
        if (expense is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Despesa nao encontrada.");

        var description = expense.Description;

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync(ct);

        await _tripNotifier.NotifyExpenseDeletedAsync(tripId, expenseId, ct);

        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ExpenseDeleted, nameof(Expense), expenseId,
            $"{actorName} removeu o gasto \"{description}\".", ct);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<SettlementDto> GetSettlementAsync(Guid tripId, CancellationToken ct = default)
    {
        var expenses = await _db.Expenses.AsNoTracking()
            .Include(e => e.Splits)
            .Where(e => e.TripId == tripId)
            .ToListAsync(ct);

        var settlementRecords = await _db.SettlementRecords.AsNoTracking().Where(s => s.TripId == tripId).ToListAsync(ct);
        var confirmed = settlementRecords.Where(s => s.Status == SettlementRecordStatus.Confirmed).ToList();
        var pending = settlementRecords.Where(s => s.Status == SettlementRecordStatus.Pending).ToList();

        var balances = SettlementCalculator.CalculateBalances(expenses, confirmed);
        var transfers = SettlementCalculator.Simplify(balances);

        return new SettlementDto(
            balances.Select(b => new ParticipantBalanceDto(b.ParticipantId, b.TotalPaid, b.TotalOwed, b.Net)).ToList(),
            transfers.Select(t => new SettlementTransferDto(t.FromParticipantId, t.ToParticipantId, t.Amount)).ToList(),
            pending.Select(p => new SettlementRecordDto(p.Id, p.FromParticipantId, p.ToParticipantId, p.Amount, p.Status, p.CreatedAt, p.ConfirmedAt)).ToList());
    }

    /// <summary>So notifica no exato momento em que o total gasto na categoria cruza o
    /// planejado - nao a cada gasto novo lancado numa categoria ja estourada (senao viraria
    /// spam a cada lancamento seguinte).</summary>
    private async Task NotifyIfBudgetJustExceededAsync(Guid tripId, Guid actorUserId, string category, decimal newExpenseAmount, string currency, CancellationToken ct)
    {
        var plannedAmount = await _db.Budgets.AsNoTracking()
            .Where(b => b.TripId == tripId && b.Category == category)
            .Select(b => (decimal?)b.PlannedAmount)
            .FirstOrDefaultAsync(ct);

        if (plannedAmount is not { } planned)
            return;

        var totalSpent = await _db.Expenses.AsNoTracking()
            .Where(e => e.TripId == tripId && e.Category == category)
            .SumAsync(e => e.Amount, ct);

        var spentBeforeThisExpense = totalSpent - newExpenseAmount;
        if (spentBeforeThisExpense > planned || totalSpent <= planned)
            return;

        await _notificationService.NotifyTripAsync(tripId, actorUserId, NotificationType.BudgetExceeded,
            $"O orcamento de {category} foi estourado: {totalSpent.ToString("0.00", CultureInfo.InvariantCulture)} de " +
            $"{planned.ToString("0.00", CultureInfo.InvariantCulture)} {currency}.", ct);
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
