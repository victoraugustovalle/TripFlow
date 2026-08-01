using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

/// <summary>Registro de uma quitacao real entre dois participantes, fora do app (Pix, dinheiro
/// na mao...). Criado quando o devedor marca uma transferencia sugerida pelo
/// SettlementCalculator como paga (Pending) - so passa a valer no calculo de saldo depois que
/// o credor confirma que recebeu (Confirmed). Por honestidade mutua, nao ha gateway de
/// pagamento real por tras disso.</summary>
public class SettlementRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    /// <summary>Quem pagou (o devedor, segundo o saldo calculado).</summary>
    public Guid FromParticipantId { get; set; }
    public TripParticipant? FromParticipant { get; set; }

    /// <summary>Quem recebeu (o credor) - so ele pode confirmar essa quitacao.</summary>
    public Guid ToParticipantId { get; set; }
    public TripParticipant? ToParticipant { get; set; }

    public decimal Amount { get; set; }
    public SettlementRecordStatus Status { get; set; } = SettlementRecordStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
}
