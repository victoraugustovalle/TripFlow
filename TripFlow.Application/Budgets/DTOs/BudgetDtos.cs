namespace TripFlow.Application.Budgets.DTOs;

public record UpsertBudgetRequest(string Category, decimal PlannedAmount);

public record BudgetDto(Guid Id, string Category, decimal PlannedAmount, decimal ActualAmount);
