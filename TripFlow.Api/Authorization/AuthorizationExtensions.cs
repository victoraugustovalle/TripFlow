using Microsoft.AspNetCore.Authorization;
using TripFlow.Domain.Enums;

namespace TripFlow.Api.Authorization;

public static class AuthorizationExtensions
{
    public const string TripViewerPolicy = "TripViewer";
    public const string TripEditorPolicy = "TripEditor";
    public const string TripOwnerPolicy = "TripOwner";

    public static IServiceCollection AddTripAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationHandler, TripRoleAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(TripViewerPolicy, p => p.Requirements.Add(new TripRoleRequirement(TripRole.Viewer)))
            .AddPolicy(TripEditorPolicy, p => p.Requirements.Add(new TripRoleRequirement(TripRole.Editor)))
            .AddPolicy(TripOwnerPolicy, p => p.Requirements.Add(new TripRoleRequirement(TripRole.Owner)));

        return services;
    }
}
