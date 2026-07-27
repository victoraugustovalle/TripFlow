using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

/// <summary>
/// Representa tanto o convite quanto a participacao em si: comeca com UserId nulo
/// (so o e-mail convidado existe) e ganha UserId quando alguem com aquele e-mail
/// aceita (seja um usuario ja existente, seja alguem que acabou de se cadastrar).
/// </summary>
public class TripParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public required string InvitedEmail { get; set; }
    public string? DisplayName { get; set; }

    public TripRole Role { get; set; } = TripRole.Viewer;
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Invited;

    public string? InviteTokenHash { get; set; }
    public DateTime? InviteTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
}
