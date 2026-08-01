namespace TripFlow.Application.Journal.DTOs;

public record JournalEntryDto(
    Guid TripId,
    string Name,
    string? Destination,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? CoverImageUrl,
    string Currency,
    decimal TotalSpent,
    double? AverageRating);

/// <summary>DestinationsCount conta destinos distintos (case-insensitive, ignorando os em
/// branco); TotalDaysTraveled soma a duracao (EndDate - StartDate + 1) das viagens que tem
/// as duas datas preenchidas.</summary>
public record MyJournalDto(int TripsCompletedCount, int DestinationsCount, int TotalDaysTraveled, IReadOnlyList<JournalEntryDto> Trips);
