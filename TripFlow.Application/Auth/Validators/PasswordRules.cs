using FluentValidation;

namespace TripFlow.Application.Auth.Validators;

public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(this IRuleBuilder<T, string> rule, int minLength)
    {
        return rule
            .MinimumLength(minLength).WithMessage($"A senha precisa ter no minimo {minLength} caracteres.")
            .Matches("[A-Z]").WithMessage("A senha precisa ter ao menos uma letra maiuscula.")
            .Matches("[a-z]").WithMessage("A senha precisa ter ao menos uma letra minuscula.")
            .Matches("[0-9]").WithMessage("A senha precisa ter ao menos um numero.")
            .Matches(@"[\W_]").WithMessage("A senha precisa ter ao menos um caractere especial.");
    }
}
