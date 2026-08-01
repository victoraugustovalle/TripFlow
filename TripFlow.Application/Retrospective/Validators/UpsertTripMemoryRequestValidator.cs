using FluentValidation;
using TripFlow.Application.Retrospective.DTOs;

namespace TripFlow.Application.Retrospective.Validators;

public class UpsertTripMemoryRequestValidator : AbstractValidator<UpsertTripMemoryRequest>
{
    public UpsertTripMemoryRequestValidator()
    {
        RuleFor(x => x.Highlight).MaximumLength(500);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
    }
}
