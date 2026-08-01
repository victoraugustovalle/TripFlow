using FluentValidation;
using TripFlow.Application.Expenses.DTOs;

namespace TripFlow.Application.Expenses.Validators;

public class MarkSettlementPaidRequestValidator : AbstractValidator<MarkSettlementPaidRequest>
{
    public MarkSettlementPaidRequestValidator()
    {
        RuleFor(x => x.ToParticipantId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
