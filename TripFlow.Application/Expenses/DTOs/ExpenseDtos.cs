namespace TripFlow.Application.Expenses.DTOs;

public record CreateExpenseRequest(
    string Description,
    decimal Amount,
    string Category,
    Guid PaidByParticipantId,
    DateOnly ExpenseDate,
    List<Guid>? SplitBetweenParticipantIds);

public record ExpenseSplitDto(Guid ParticipantId, decimal ShareAmount);

public record ExpenseDto(
    Guid Id,
    string Description,
    decimal Amount,
    string Category,
    Guid PaidByParticipantId,
    DateOnly ExpenseDate,
    DateTime CreatedAt,
    IReadOnlyList<ExpenseSplitDto> Splits);

public record ParticipantBalanceDto(Guid ParticipantId, decimal TotalPaid, decimal TotalOwed, decimal Net);
public record SettlementTransferDto(Guid FromParticipantId, Guid ToParticipantId, decimal Amount);
public record SettlementDto(IReadOnlyList<ParticipantBalanceDto> Balances, IReadOnlyList<SettlementTransferDto> Transfers);
