using TripFlow.Domain.Enums;

namespace TripFlow.Application.Reservations.DTOs;

public record CreateReservationRequest(
    ReservationType Type,
    string Title,
    string? ProviderName,
    string? ConfirmationCode,
    DateTime? StartAt,
    DateTime? EndAt,
    string? Location,
    double? Latitude,
    double? Longitude,
    decimal? Price,
    string? Currency,
    string? Notes,
    Guid? ItineraryItemId);

public record UpdateReservationRequest(
    ReservationType Type,
    string Title,
    string? ProviderName,
    string? ConfirmationCode,
    DateTime? StartAt,
    DateTime? EndAt,
    string? Location,
    double? Latitude,
    double? Longitude,
    decimal? Price,
    string? Currency,
    string? Notes,
    Guid? ItineraryItemId);

public record ReservationDto(
    Guid Id,
    ReservationType Type,
    string Title,
    string? ProviderName,
    string? ConfirmationCode,
    DateTime? StartAt,
    DateTime? EndAt,
    string? Location,
    double? Latitude,
    double? Longitude,
    decimal? Price,
    string? Currency,
    string? Notes,
    Guid? ItineraryItemId,
    DateTime CreatedAt);
