namespace TripFlow.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = "Geral";

    public Guid PaidByParticipantId { get; set; }
    public TripParticipant? PaidByParticipant { get; set; }

    public DateOnly ExpenseDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Snapshot de quem divide essa despesa no momento do lancamento - nao muda
    /// se alguem entrar ou sair da viagem depois.</summary>
    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
}
