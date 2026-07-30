using TripFlow.Domain.Enums;

namespace TripFlow.Application.Trips.DTOs;

public record CreateTripRequest(string Name, string? Destination, string? Description, DateOnly? StartDate, DateOnly? EndDate, string Currency = "BRL");
public record UpdateTripRequest(string Name, string? Destination, string? Description, DateOnly? StartDate, DateOnly? EndDate, TripStatus Status, string? CoverImageUrl);

public record TripDto(
    Guid Id,
    string Name,
    string? Destination,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Currency,
    TripStatus Status,
    string? CoverImageUrl,
    Guid CreatedByUserId,
    DateTime CreatedAt);

public record TripSummaryDto(Guid Id, string Name, string? Destination, TripStatus Status, DateOnly? StartDate, DateOnly? EndDate, string? CoverImageUrl, TripRole MyRole);
