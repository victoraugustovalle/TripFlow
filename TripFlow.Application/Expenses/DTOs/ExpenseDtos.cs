using TripFlow.Domain.Enums;

namespace TripFlow.Application.Expenses.DTOs;

/// <summary>Filtros da listagem de gastos, todos opcionais - Search casa com descricao OU
/// categoria (nao precisa saber a grafia exata da categoria pra achar um gasto).</summary>
public record ExpenseListQuery(string? Category, DateOnly? From, DateOnly? To, string? Search, int Page = 1, int PageSize = 20);

public record ExpenseShareInput(Guid ParticipantId, decimal ShareAmount);

public record CreateExpenseRequest(
    string Description,
    decimal Amount,
    string Category,
    Guid PaidByParticipantId,
    DateOnly ExpenseDate,
    List<Guid>? SplitBetweenParticipantIds,
    /// <summary>Divisao customizada por participante - quando informada, tem prioridade sobre
    /// SplitBetweenParticipantIds e a soma dos valores precisa bater com Amount.</summary>
    List<ExpenseShareInput>? Shares);

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

public record SettlementRecordDto(
    Guid Id, Guid FromParticipantId, Guid ToParticipantId, decimal Amount,
    SettlementRecordStatus Status, DateTime CreatedAt, DateTime? ConfirmedAt);

public record SettlementDto(
    IReadOnlyList<ParticipantBalanceDto> Balances,
    IReadOnlyList<SettlementTransferDto> Transfers,
    /// <summary>Quitacoes marcadas como pagas mas que ainda aguardam confirmacao de quem
    /// recebeu - o par (From, To) continua aparecendo em Transfers ate a confirmacao (o
    /// debito so sai do saldo depois que o credor confirma), mas o frontend usa isso pra
    /// trocar o botao "Marcar como pago" por um badge de "aguardando confirmacao".</summary>
    IReadOnlyList<SettlementRecordDto> PendingSettlements);

public record MarkSettlementPaidRequest(Guid ToParticipantId, decimal Amount);
