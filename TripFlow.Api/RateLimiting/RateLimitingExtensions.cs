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

            // Login e refresh: o alvo natural de brute force / credential stuffing, fica
            // no limite mais apertado. Registro/confirmacao/reset ficam num limite proprio,
            // mais folgado - senao um fluxo legitimo (registrar -> confirmar -> logar) ja
            // esbarra no limite sozinho.
            options.AddPolicy("auth-login", context => RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: GetPartitionKey(context),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueLimit = 0
                }));

            options.AddPolicy("auth", context => RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: GetPartitionKey(context),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 15,
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
