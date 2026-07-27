namespace TripFlow.Domain.Entities;

/// <summary>Valor planejado por categoria - o gasto real vem da soma dos Expenses da mesma categoria.</summary>
public class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public required string Category { get; set; }
    public decimal PlannedAmount { get; set; }
}
