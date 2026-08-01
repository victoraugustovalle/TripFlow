using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

/// <summary>Preferencia de silenciar um tipo de notificacao numa viagem especifica - a presenca
/// de uma linha (UserId, TripId, Type) significa "nao me notifique mais desse tipo aqui".
/// Sem linha = notifica normalmente (opt-out, nao opt-in).</summary>
public class NotificationMute
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public NotificationType Type { get; set; }
}
