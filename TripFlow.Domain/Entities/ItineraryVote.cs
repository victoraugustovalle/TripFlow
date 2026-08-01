namespace TripFlow.Domain.Entities;

/// <summary>Voto de um participante numa opcao de uma proposta de roteiro. ItineraryItemId e
/// redundante com Option.ItineraryItemId de proposito: permite um indice unico
/// (ItineraryItemId, ParticipantId) que garante um voto por participante por proposta (nao por
/// opcao) - trocar de opcao e so atualizar o OptionId desse registro, nunca inserir um segundo.</summary>
public class ItineraryVote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ItineraryItemId { get; set; }
    public ItineraryItem? ItineraryItem { get; set; }

    public Guid OptionId { get; set; }
    public ItineraryProposalOption? Option { get; set; }

    public Guid ParticipantId { get; set; }
    public TripParticipant? Participant { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
