using FluentValidation;
using Microsoft.Extensions.Options;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.Common;

namespace TripFlow.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator(IOptions<AuthPolicyOptions> policy)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MustBeStrongPassword(policy.Value.MinPasswordLength);
    }
}
