using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripFlow.Application.Activity;
using TripFlow.Application.Auth;
using TripFlow.Application.Budgets;
using TripFlow.Application.Checklist;
using TripFlow.Application.Common;
using TripFlow.Application.Documents;
using TripFlow.Application.Expenses;
using TripFlow.Application.Itinerary;
using TripFlow.Application.Journal;
using TripFlow.Application.Notifications;
using TripFlow.Application.Overview;
using TripFlow.Application.Participants;
using TripFlow.Application.Reservations;
using TripFlow.Application.Retrospective;
using TripFlow.Application.Trips;
using TripFlow.Application.TwoFactor;

namespace TripFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthPolicyOptions>(configuration.GetSection(AuthPolicyOptions.SectionName));
        services.Configure<TwoFactorOptions>(configuration.GetSection(TwoFactorOptions.SectionName));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<AuthService>();
        services.AddScoped<TripService>();
        services.AddScoped<ParticipantService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<BudgetService>();
        services.AddScoped<ChecklistService>();
        services.AddScoped<ItineraryService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<TwoFactorService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<ActivityService>();
        services.AddScoped<SettlementService>();
        services.AddScoped<RetrospectiveService>();
        services.AddScoped<TripMemoryService>();
        services.AddScoped<TripReadinessService>();
        services.AddScoped<OverviewService>();
        services.AddScoped<JournalService>();

        return services;
    }
}
