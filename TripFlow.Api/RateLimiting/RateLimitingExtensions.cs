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

            // Login: o alvo natural de brute force / credential stuffing, fica no limite
            // mais apertado.
            options.AddPolicy("auth-login", context => RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: GetPartitionKey(context),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueLimit = 0
                }));

            // Refresh nao e alvo de brute force (a "credencial" e um token aleatorio de 256
            // bits num cookie httpOnly, nao uma senha adivinhavel) e roda automaticamente a
            // cada carregamento de pagina - um usuario que so da F5 algumas vezes nao pode
            // cair no mesmo limite de quem esta tentando adivinhar senha.
            options.AddPolicy("auth-refresh", context => RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: GetPartitionKey(context),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 30,
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

            // Geocoding (Nominatim): a politica de uso deles exige no maximo 1 req/segundo
            // pro servico inteiro, nao por usuario - por isso a chave de particao e uma
            // constante, nao o IP de quem chamou. Um pouco de fila (em vez de rejeitar na
            // hora) absorve picos pequenos sem estourar o limite deles.
            options.AddPolicy("geocoding", _ => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: "nominatim-global",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 3
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
