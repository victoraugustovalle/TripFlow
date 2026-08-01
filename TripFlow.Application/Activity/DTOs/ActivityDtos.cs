using TripFlow.Domain.Enums;

namespace TripFlow.Application.Activity.DTOs;

public record ActivityLogEntryDto(
    Guid Id,
    ActivityType Type,
    string EntityType,
    Guid? EntityId,
    string Message,
    string? ActorDisplayName,
    // Usuario que fez a acao - o frontend usa isso pra nao mostrar um toast de tempo real pra
    // voce mesmo sobre a sua propria acao (que ja teve feedback na hora, via toast de sucesso
    // da mutation).
    Guid? ActorUserId,
    DateTime CreatedAt);
