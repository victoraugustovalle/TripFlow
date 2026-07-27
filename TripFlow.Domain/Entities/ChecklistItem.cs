namespace TripFlow.Domain.Entities;

public class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public required string Title { get; set; }
    public bool IsDone { get; set; }

    public Guid? AssignedToParticipantId { get; set; }
    public TripParticipant? AssignedToParticipant { get; set; }

    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
