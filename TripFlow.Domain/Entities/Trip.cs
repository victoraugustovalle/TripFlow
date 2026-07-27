using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Destination { get; set; }
    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Currency { get; set; } = "BRL";
    public TripStatus Status { get; set; } = TripStatus.Planning;

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TripParticipant> Participants { get; set; } = new List<TripParticipant>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
}
