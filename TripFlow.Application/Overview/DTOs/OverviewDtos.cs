using TripFlow.Application.Expenses.DTOs;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Overview.DTOs;

public record BudgetCategoryOverviewDto(string Category, decimal PlannedAmount, decimal ActualAmount);

public record BudgetOverviewDto(decimal TotalPlanned, decimal TotalActual, IReadOnlyList<BudgetCategoryOverviewDto> Categories);

public record ChecklistOverviewDto(int TotalCount, int DoneCount);

/// <summary>Recorte do settlement da viagem sob o ponto de vista de quem esta pedindo - so as
/// transferencias e quitacoes pendentes que envolvem o participante atual.</summary>
public record SettlementOverviewDto(
    decimal MyNetAmount, IReadOnlyList<SettlementTransferDto> MyTransfers, IReadOnlyList<SettlementRecordDto> MyPendingSettlements);

public record UpcomingItineraryItemDto(Guid Id, string Title, ItineraryItemType Type, DateOnly ItemDate, TimeOnly? StartTime, string? Location);

/// <summary>Uma pendencia que impede a viagem de estar 100% pronta - Key identifica a regra
/// (pra debug/telemetria), Label e o texto pronto pro usuario e TargetTab e o valor exato da
/// aba do frontend (TripDetailPage.Tab) pra onde o card de Prontidao deve navegar ao clicar.</summary>
public record TripReadinessItemDto(string Key, string Label, string TargetTab);

/// <summary>PercentReady e calculado sobre as regras aplicaveis a essa viagem especifica (ex: a
/// regra do passaporte so entra na conta se houver reserva de voo internacional) - por isso nao
/// da pra derivar o percentual so contando PendingItems, o backend ja manda pronto.</summary>
public record TripReadinessDto(int PercentReady, IReadOnlyList<TripReadinessItemDto> PendingItems);

public record TripOverviewDto(
    int PendingInvitesCount,
    BudgetOverviewDto Budget,
    ChecklistOverviewDto Checklist,
    SettlementOverviewDto Settlement,
    IReadOnlyList<UpcomingItineraryItemDto> UpcomingItineraryItems,
    TripReadinessDto Readiness);
