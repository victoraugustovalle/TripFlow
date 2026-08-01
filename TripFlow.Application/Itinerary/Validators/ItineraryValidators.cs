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

public class CreateItineraryProposalOptionRequestValidator : AbstractValidator<CreateItineraryProposalOptionRequest>
{
    public CreateItineraryProposalOptionRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude is not null);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude is not null);
    }
}

public class CreateItineraryProposalRequestValidator : AbstractValidator<CreateItineraryProposalRequest>
{
    public CreateItineraryProposalRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime!.Value)
            .When(x => x.StartTime is not null && x.EndTime is not null)
            .WithMessage("O horario final nao pode ser antes do horario inicial.");

        // 2-3 opcoes e o caso de uso tipico ("restaurante A ou B") - o teto de 5 e so pra nao
        // virar um mini sistema de enquetes genericas, nao uma regra de negocio de verdade.
        RuleFor(x => x.Options).Must(o => o.Count is >= 2 and <= 5)
            .WithMessage("Uma proposta precisa de 2 a 5 opcoes.");
        RuleForEach(x => x.Options).SetValidator(new CreateItineraryProposalOptionRequestValidator());
    }
}

public class VoteItineraryProposalRequestValidator : AbstractValidator<VoteItineraryProposalRequest>
{
    public VoteItineraryProposalRequestValidator()
    {
        RuleFor(x => x.OptionId).NotEqual(Guid.Empty);
    }
}

public class ConfirmItineraryProposalRequestValidator : AbstractValidator<ConfirmItineraryProposalRequest>
{
    public ConfirmItineraryProposalRequestValidator()
    {
        RuleFor(x => x.OptionId).NotEqual(Guid.Empty);
    }
}
