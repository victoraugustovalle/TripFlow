namespace TripFlow.Domain.Entities;

public class ExpenseSplit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExpenseId { get; set; }
    public Expense? Expense { get; set; }

    public Guid ParticipantId { get; set; }
    public TripParticipant? Participant { get; set; }

    public decimal ShareAmount { get; set; }
}
