using FluentValidation;
using Microsoft.Extensions.Options;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.Common;

namespace TripFlow.Application.Auth.Validators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator(IOptions<AuthPolicyOptions> policy)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MustBeStrongPassword(policy.Value.MinPasswordLength);
    }
}
