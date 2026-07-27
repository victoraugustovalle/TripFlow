using TripFlow.Application.Abstractions;

namespace TripFlow.Api.RealTime;

public static class RealTimeExtensions
{
    public const string HubPath = "/hubs/trip";

    public static IServiceCollection AddTripFlowRealTime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<ITripNotifier, SignalRTripNotifier>();
        return services;
    }

    public static IEndpointRouteBuilder MapTripFlowRealTime(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<TripHub>(HubPath);
        return endpoints;
    }
}
