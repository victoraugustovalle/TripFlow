using FluentValidation;
using TripFlow.Application.Checklist.DTOs;

namespace TripFlow.Application.Checklist.Validators;

public class CreateChecklistItemRequestValidator : AbstractValidator<CreateChecklistItemRequest>
{
    public CreateChecklistItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
    }
}

public class UpdateChecklistItemRequestValidator : AbstractValidator<UpdateChecklistItemRequest>
{
    public UpdateChecklistItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
    }
}
