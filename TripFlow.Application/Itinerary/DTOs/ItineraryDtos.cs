using TripFlow.Domain.Enums;

namespace TripFlow.Application.Itinerary.DTOs;

public record CreateItineraryItemRequest(
    string Title,
    string? Description,
    ItineraryItemType Type,
    DateOnly ItemDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Location,
    double? Latitude,
    double? Longitude);

public record UpdateItineraryItemRequest(
    string Title,
    string? Description,
    ItineraryItemType Type,
    DateOnly ItemDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Location,
    double? Latitude,
    double? Longitude);

public record ItineraryItemDto(
    Guid Id,
    string Title,
    string? Description,
    ItineraryItemType Type,
    DateOnly ItemDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Location,
    double? Latitude,
    double? Longitude,
    DateTime CreatedAt);
