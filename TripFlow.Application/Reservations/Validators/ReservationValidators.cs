using FluentValidation;
using TripFlow.Application.Reservations.DTOs;

namespace TripFlow.Application.Reservations.Validators;

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ProviderName).MaximumLength(200);
        RuleFor(x => x.ConfirmationCode).MaximumLength(80);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price is not null);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude is not null);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude is not null);
        RuleFor(x => x.EndAt)
            .GreaterThanOrEqualTo(x => x.StartAt!.Value)
            .When(x => x.StartAt is not null && x.EndAt is not null)
            .WithMessage("O fim nao pode ser antes do inicio.");
    }
}

public class UpdateReservationRequestValidator : AbstractValidator<UpdateReservationRequest>
{
    public UpdateReservationRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ProviderName).MaximumLength(200);
        RuleFor(x => x.ConfirmationCode).MaximumLength(80);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price is not null);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude is not null);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude is not null);
        RuleFor(x => x.EndAt)
            .GreaterThanOrEqualTo(x => x.StartAt!.Value)
            .When(x => x.StartAt is not null && x.EndAt is not null)
            .WithMessage("O fim nao pode ser antes do inicio.");
    }
}
