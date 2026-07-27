using FluentValidation;
using TripFlow.Application.Budgets.DTOs;

namespace TripFlow.Application.Budgets.Validators;

public class UpsertBudgetRequestValidator : AbstractValidator<UpsertBudgetRequest>
{
    public UpsertBudgetRequestValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
        RuleFor(x => x.PlannedAmount).GreaterThanOrEqualTo(0);
    }
}
