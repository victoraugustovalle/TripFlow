namespace TripFlow.Domain.Entities;

/// <summary>Marca que um aviso proativo especifico ja foi disparado pra uma Trip, pra o job
/// diario (ProactiveNotificationService) nao notificar a mesma coisa de novo a cada execucao.
/// Key identifica o aviso dentro da viagem (ex.: "readiness:3" = "faltam 3 dias e nao esta
/// pronta", "budget-pace:Hospedagem" = ritmo de gasto da categoria Hospedagem) - a mesma Key
/// so dispara notificacao uma vez por Trip.</summary>
public class ProactiveNudgeLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public required string Key { get; set; }
    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
}
