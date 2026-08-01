using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Budgets;
using TripFlow.Application.Overview;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Notifications;

/// <summary>
/// Chamado uma vez por dia (ver hosted service no Api) pra transformar sinais que hoje so
/// existem sob demanda - TripReadinessService, orcamento planejado vs. gasto - em avisos
/// proativos: o sistema passa a falar com o usuario por conta propria, em vez de so responder
/// quando alguem abre a tela certa. Cada aviso e identificado por uma Key (ProactiveNudgeLog) e
/// so dispara uma vez por viagem, senao toda execucao do job repetiria a mesma notificacao.
/// </summary>
public class ProactiveNotificationService
{
    private static readonly int[] ReadinessReminderDaysBeforeStart = [7, 3, 1];

    // Ritmo de gasto so vira aviso quando a fatia ja usada do orcamento esta pelo menos 20
    // pontos percentuais a frente da fatia decorrida da viagem - abaixo disso e ritmo normal.
    private const decimal BudgetPaceThreshold = 0.2m;

    private readonly IAppDbContext _db;
    private readonly TripReadinessService _readinessService;
    private readonly BudgetService _budgetService;
    private readonly NotificationService _notificationService;

    public ProactiveNotificationService(
        IAppDbContext db, TripReadinessService readinessService, BudgetService budgetService, NotificationService notificationService)
    {
        _db = db;
        _readinessService = readinessService;
        _budgetService = budgetService;
        _notificationService = notificationService;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await CheckReadinessRemindersAsync(today, ct);
        await CheckBudgetPaceAsync(today, ct);
    }

    /// <summary>Viagens em planejamento que comecam em exatamente 7, 3 ou 1 dia(s) e ainda nao
    /// estao 100% prontas recebem um resumo das pendencias, reaproveitando as mesmas regras que
    /// a aba Overview ja calcula sob demanda.</summary>
    private async Task CheckReadinessRemindersAsync(DateOnly today, CancellationToken ct)
    {
        var horizon = today.AddDays(ReadinessReminderDaysBeforeStart.Max());
        var candidateTrips = await _db.Trips.AsNoTracking()
            .Where(t => t.Status == TripStatus.Planning && t.StartDate != null && t.StartDate >= today && t.StartDate <= horizon)
            .ToListAsync(ct);

        foreach (var trip in candidateTrips)
        {
            var daysUntilStart = trip.StartDate!.Value.DayNumber - today.DayNumber;
            if (!ReadinessReminderDaysBeforeStart.Contains(daysUntilStart))
                continue;

            var key = $"readiness:{daysUntilStart}";
            if (await _db.ProactiveNudgeLogs.AnyAsync(l => l.TripId == trip.Id && l.Key == key, ct))
                continue;

            var readiness = await _readinessService.GetAsync(trip.Id, ct);
            if (readiness.PercentReady >= 100)
                continue;

            var dayWord = daysUntilStart == 1 ? "dia" : "dias";
            var topPending = string.Join("; ", readiness.PendingItems.Take(2).Select(p => p.Label));
            var extraCount = readiness.PendingItems.Count - 2;
            var extra = extraCount > 0 ? $"; e mais {extraCount} pendencia(s)" : "";

            var message = $"Faltam {daysUntilStart} {dayWord} para \"{trip.Name}\" e a viagem ainda nao esta pronta " +
                          $"({readiness.PercentReady}%): {topPending}{extra}.";

            await _notificationService.NotifyTripAsync(trip.Id, null, NotificationType.TripStartingSoonNotReady, message, ct);
            _db.ProactiveNudgeLogs.Add(new ProactiveNudgeLog { TripId = trip.Id, Key = key });
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Viagens em andamento com pelo menos uma categoria de orcamento definida: compara
    /// a fatia do planejado ja gasta com a fatia da viagem ja decorrida. Se o gasto esta bem a
    /// frente do calendario (e ainda nao estourou - isso ja e avisado por NotifyIfBudgetJustExceededAsync
    /// no ExpenseService), avisa que o ritmo atual deve estourar a categoria.</summary>
    private async Task CheckBudgetPaceAsync(DateOnly today, CancellationToken ct)
    {
        var ongoingTrips = await _db.Trips.AsNoTracking()
            .Where(t => t.Status == TripStatus.Ongoing && t.StartDate != null && t.EndDate != null)
            .ToListAsync(ct);

        foreach (var trip in ongoingTrips)
        {
            var totalDays = trip.EndDate!.Value.DayNumber - trip.StartDate!.Value.DayNumber;
            if (totalDays <= 0)
                continue;

            var elapsedDays = today.DayNumber - trip.StartDate.Value.DayNumber;
            var elapsedRatio = Math.Clamp((decimal)elapsedDays / totalDays, 0m, 1m);

            // Mesmo DTO que a aba Orcamento ja mostra (planejado x realizado por categoria) -
            // nenhuma regra nova aqui, so um gatilho por tempo em cima do que ja existe.
            var budgets = await _budgetService.ListAsync(trip.Id, ct);

            foreach (var budget in budgets)
            {
                if (budget.PlannedAmount <= 0)
                    continue;

                var spentRatio = budget.ActualAmount / budget.PlannedAmount;
                if (spentRatio >= 1m || spentRatio - elapsedRatio < BudgetPaceThreshold)
                    continue;

                var key = $"budget-pace:{budget.Category}";
                if (await _db.ProactiveNudgeLogs.AnyAsync(l => l.TripId == trip.Id && l.Key == key, ct))
                    continue;

                var spentPercent = (int)Math.Round(spentRatio * 100);
                var elapsedPercent = (int)Math.Round(elapsedRatio * 100);
                var message = $"No ritmo atual, o orcamento de {budget.Category} em \"{trip.Name}\" deve estourar antes do fim da " +
                              $"viagem (ja usou {spentPercent}% do planejado com {elapsedPercent}% da viagem decorrida).";

                await _notificationService.NotifyTripAsync(trip.Id, null, NotificationType.BudgetPaceExceeding, message, ct);
                _db.ProactiveNudgeLogs.Add(new ProactiveNudgeLog { TripId = trip.Id, Key = key });
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
