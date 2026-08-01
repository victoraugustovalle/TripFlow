using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

/// <summary>Notificacao in-app de um evento relevante pro usuario dentro de uma viagem
/// especifica (convite aceito, item de checklist atribuido...). Texto pronto, sem payload
/// tipado por evento - simples de listar e marcar como lida. Type existe so pra permitir
/// silenciar por categoria (ver NotificationMute), a UI continua so mostrando Message.</summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public Guid RecipientUserId { get; set; }
    public User? RecipientUser { get; set; }

    public NotificationType Type { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
