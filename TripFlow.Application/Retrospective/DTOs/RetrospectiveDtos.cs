namespace TripFlow.Application.Retrospective.DTOs;

public record TopSpenderDto(Guid ParticipantId, string DisplayName, decimal TotalPaid);

public record TripMemoryDto(Guid ParticipantId, string DisplayName, string? Highlight, int? Rating, string? PhotoUrl, DateTime UpdatedAt);

public record UpsertTripMemoryRequest(string? Highlight, int? Rating, string? PhotoUrl);

public record TripRetrospectiveDto(
    string TripName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? DurationDays,
    decimal TotalSpent,
    decimal TotalPlanned,
    IReadOnlyList<TopSpenderDto> TopSpenders,
    int ChecklistTotalCount,
    int ChecklistDoneCount,
    int ItineraryItemsCount,
    int ParticipantsCount,
    IReadOnlyList<TripMemoryDto> Memories,
    double? AverageRating);
