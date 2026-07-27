using FluentValidation;
using TripFlow.Application.Itinerary.DTOs;

namespace TripFlow.Application.Itinerary.Validators;

public class CreateItineraryItemRequestValidator : AbstractValidator<CreateItineraryItemRequest>
{
    public CreateItineraryItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude is not null);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude is not null);
        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime!.Value)
            .When(x => x.StartTime is not null && x.EndTime is not null)
            .WithMessage("O horario final nao pode ser antes do horario inicial.");
    }
}

public class UpdateItineraryItemRequestValidator : AbstractValidator<UpdateItineraryItemRequest>
{
    public UpdateItineraryItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude is not null);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude is not null);
        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime!.Value)
            .When(x => x.StartTime is not null && x.EndTime is not null)
            .WithMessage("O horario final nao pode ser antes do horario inicial.");
    }
}
