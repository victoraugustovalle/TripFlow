using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

/// <summary>Registro cronologico de eventos relevantes da viagem (gasto lancado, item do
/// checklist atribuido, item do roteiro criado...) - sustenta a timeline da Overview. Gravado
/// nos mesmos pontos onde os services ja chamam ITripNotifier, com a mensagem e o autor ja
/// resolvidos no momento (mesmo padrao de Notification.Message) pra nao depender de um
/// participante que pode ter sido removido depois.</summary>
public class ActivityLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    /// <summary>Quem fez a acao - nulo quando o evento nao tem um autor claro (ex: acao de um Admin global sem vinculo com a viagem).</summary>
    public Guid? ActorParticipantId { get; set; }
    public TripParticipant? ActorParticipant { get; set; }

    public ActivityType Type { get; set; }

    /// <summary>Tipo e id da entidade afetada (ex: "Expense" + guid do gasto), pra o frontend linkar direto pra ela.</summary>
    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }

    public required string Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
