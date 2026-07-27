namespace TripFlow.Domain.Entities;

/// <summary>Codigo de uso unico pra entrar se o usuario perder acesso ao app autenticador. So o hash fica salvo - o codigo em claro e mostrado uma vez so, na hora que o 2FA e ativado.</summary>
public class TwoFactorRecoveryCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public required string CodeHash { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
