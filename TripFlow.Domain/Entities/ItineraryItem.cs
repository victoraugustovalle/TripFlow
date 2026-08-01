using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

public class ItineraryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }
    public ItineraryItemType Type { get; set; } = ItineraryItemType.Activity;

    public DateOnly ItemDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ItineraryItemStatus Status { get; set; } = ItineraryItemStatus.Confirmed;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    /// <summary>So relevante quando Status=Proposed - as alternativas concorrentes pro mesmo
    /// slot (data/horario/tipo). Confirmar a proposta esvazia essa lista: o item vira "normal",
    /// com os campos de local/coordenada copiados da opcao vencedora.</summary>
    public ICollection<ItineraryProposalOption> ProposalOptions { get; set; } = new List<ItineraryProposalOption>();
}
