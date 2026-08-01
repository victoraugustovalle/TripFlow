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
    DateTime CreatedAt,
    ItineraryItemStatus Status,
    IReadOnlyList<ItineraryProposalOptionDto> ProposalOptions,
    Guid? MyVotedOptionId);

public record CreateItineraryProposalOptionRequest(string Title, string? Description, string? Location, double? Latitude, double? Longitude);

public record CreateItineraryProposalRequest(
    string Title,
    string? Description,
    ItineraryItemType Type,
    DateOnly ItemDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    IReadOnlyList<CreateItineraryProposalOptionRequest> Options);

public record VoteItineraryProposalRequest(Guid OptionId);

public record ConfirmItineraryProposalRequest(Guid OptionId);

public record ItineraryProposalOptionDto(
    Guid Id, string Title, string? Description, string? Location, double? Latitude, double? Longitude, int VoteCount);

/// <summary>Previsao do tempo pra um dia do roteiro que tem pelo menos um item com coordenadas,
/// mais sugestoes de checklist geradas a partir dela (ex.: "Guarda-chuva" se chuva e provavel) -
/// so sugere, nunca cria o item de checklist sozinho.</summary>
public record ItineraryDayWeatherDto(
    DateOnly Date,
    double? TemperatureMaxC,
    double? TemperatureMinC,
    double? PrecipitationProbabilityPercent,
    IReadOnlyList<string> SuggestedChecklistItems);
