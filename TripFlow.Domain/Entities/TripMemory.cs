namespace TripFlow.Domain.Entities;

/// <summary>Registro pessoal de um participante sobre a viagem - melhor momento, nota (1-5) e
/// uma foto marcante - pra enriquecer a Retrospectiva alem dos numeros agregados que ja existem.
/// Um por participante por viagem (upsert), editavel a qualquer momento pelo proprio dono.</summary>
public class TripMemory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public Guid ParticipantId { get; set; }
    public TripParticipant? Participant { get; set; }

    public string? Highlight { get; set; }
    public int? Rating { get; set; }

    /// <summary>URL externa ou data URL (mesmo padrao de Trip.CoverImageUrl).</summary>
    public string? PhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
