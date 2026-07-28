using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TripFlow.Api.Validation;

/// <summary>
/// Os validators do FluentValidation ficam registrados no DI (AddValidatorsFromAssembly),
/// mas isso sozinho nao os conecta em nada - precisa de algo rodando eles antes da action.
/// Esse filtro olha cada argumento da action, procura um IValidator&lt;T&gt; pro tipo dele e,
/// se achar, valida antes de deixar a action executar.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                context.Result = new BadRequestObjectResult(new { message = "Dados invalidos.", errors });
                return;
            }
        }

        await next();
    }
}
