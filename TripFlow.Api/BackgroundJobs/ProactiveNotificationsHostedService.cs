using TripFlow.Application.Notifications;

namespace TripFlow.Api.BackgroundJobs;

/// <summary>
/// Primeiro processo assincrono/agendado do projeto - ate aqui tudo era request-response.
/// Roda ProactiveNotificationService uma vez ao subir a API e depois a cada 24h. Cria um novo
/// escopo de DI a cada execucao porque os services chamados (NotificationService,
/// TripReadinessService, o proprio DbContext) sao Scoped, enquanto o hosted service em si
/// vive pelo tempo de vida do processo (Singleton).
/// </summary>
public class ProactiveNotificationsHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProactiveNotificationsHostedService> _logger;

    public ProactiveNotificationsHostedService(IServiceScopeFactory scopeFactory, ILogger<ProactiveNotificationsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            await RunOnceAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ProactiveNotificationService>().RunAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao rodar os avisos proativos agendados.");
        }
    }
}
