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

        RuleForEach(x => x.Shares).ChildRules(share =>
        {
            share.RuleFor(s => s.ParticipantId).NotEmpty();
            share.RuleFor(s => s.ShareAmount).GreaterThanOrEqualTo(0);
        });

        RuleFor(x => x)
            .Must(x => x.Shares is not { Count: > 0 } || Math.Abs(x.Shares.Sum(s => s.ShareAmount) - x.Amount) <= 0.01m)
            .WithMessage("A soma da divisao customizada precisa ser igual ao valor do gasto.")
            .WithName("Shares");
    }
}
