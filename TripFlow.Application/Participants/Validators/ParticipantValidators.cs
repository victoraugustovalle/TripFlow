using FluentValidation;
using TripFlow.Application.Participants.DTOs;

namespace TripFlow.Application.Participants.Validators;

public class InviteParticipantRequestValidator : AbstractValidator<InviteParticipantRequest>
{
    public InviteParticipantRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
