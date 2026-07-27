namespace TripFlow.Application.Checklist.DTOs;

public record CreateChecklistItemRequest(string Title, Guid? AssignedToParticipantId, DateOnly? DueDate);
public record UpdateChecklistItemRequest(string Title, bool IsDone, Guid? AssignedToParticipantId, DateOnly? DueDate);

public record ChecklistItemDto(Guid Id, string Title, bool IsDone, Guid? AssignedToParticipantId, DateOnly? DueDate, DateTime CreatedAt);
