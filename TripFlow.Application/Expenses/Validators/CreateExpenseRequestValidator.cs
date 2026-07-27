using FluentValidation;
using TripFlow.Application.Expenses.DTOs;

namespace TripFlow.Application.Expenses.Validators;

public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
        RuleFor(x => x.PaidByParticipantId).NotEmpty();
    }
}
