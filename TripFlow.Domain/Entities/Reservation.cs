using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    /// <summary>Vinculo opcional com um item do roteiro (ex: a reserva do voo aparece junto do item "Voo pra X" no dia certo).</summary>
    public Guid? ItineraryItemId { get; set; }
    public ItineraryItem? ItineraryItem { get; set; }

    public ReservationType Type { get; set; } = ReservationType.Other;
    public required string Title { get; set; }
    public string? ProviderName { get; set; }
    public string? ConfirmationCode { get; set; }

    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }

    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
