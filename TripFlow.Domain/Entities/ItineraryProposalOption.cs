namespace TripFlow.Domain.Entities;

/// <summary>Uma alternativa concorrente dentro de uma proposta de roteiro (ItineraryItem com
/// Status=Proposed) - ex.: "Restaurante A" e "Restaurante B" pro mesmo horario do dia 3.
/// Quando a proposta e confirmada, os campos da opcao vencedora sao copiados pro ItineraryItem
/// e todas as opcoes (vencedora inclusive) sao removidas.</summary>
public class ItineraryProposalOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryItemId { get; set; }
    public ItineraryItem? ItineraryItem { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public ICollection<ItineraryVote> Votes { get; set; } = new List<ItineraryVote>();
}
