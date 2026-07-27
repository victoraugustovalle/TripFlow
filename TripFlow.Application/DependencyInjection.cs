using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripFlow.Application.Auth;
using TripFlow.Application.Budgets;
using TripFlow.Application.Checklist;
using TripFlow.Application.Common;
using TripFlow.Application.Expenses;
using TripFlow.Application.Participants;
using TripFlow.Application.Trips;

namespace TripFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthPolicyOptions>(configuration.GetSection(AuthPolicyOptions.SectionName));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<AuthService>();
        services.AddScoped<TripService>();
        services.AddScoped<ParticipantService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<BudgetService>();
        services.AddScoped<ChecklistService>();

        return services;
    }
}
