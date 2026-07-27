using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TripFlow.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddTripFlowRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Login, registro, refresh, etc: bem apertado, e o alvo natural de brute force.
            options.AddPolicy("auth", context => RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: GetPartitionKey(context),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueLimit = 0
                }));

            // Resto da API: generoso o bastante pra uso normal, mas ainda com um teto.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
