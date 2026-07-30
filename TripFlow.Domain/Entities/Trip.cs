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
    /// <summary>URL externa (resultado de busca na web) ou data URL base64 (upload do dispositivo, ja redimensionado no cliente).</summary>
    public string? CoverImageUrl { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TripParticipant> Participants { get; set; } = new List<TripParticipant>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
    public ICollection<ItineraryItem> ItineraryItems { get; set; } = new List<ItineraryItem>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
