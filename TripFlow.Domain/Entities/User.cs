using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }

    /// <summary>Nulo quando o usuario so tem login via Google.</summary>
    public string? PasswordHash { get; set; }

    public string? GoogleId { get; set; }

    public GlobalRole GlobalRole { get; set; } = GlobalRole.User;

    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationCodeHash { get; set; }
    public DateTime? EmailConfirmationCodeExpiresAt { get; set; }

    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLockedOut => LockedUntil is not null && LockedUntil > DateTime.UtcNow;

    public ICollection<TripParticipant> TripParticipations { get; set; } = new List<TripParticipant>();
}
