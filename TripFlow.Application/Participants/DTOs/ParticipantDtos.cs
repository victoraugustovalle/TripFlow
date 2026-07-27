using TripFlow.Domain.Enums;

namespace TripFlow.Application.Participants.DTOs;

public record InviteParticipantRequest(string Email, TripRole Role);
public record AcceptInviteRequest(string Token);
public record UpdateParticipantRoleRequest(TripRole Role);

public record ParticipantDto(
    Guid Id,
    Guid TripId,
    Guid? UserId,
    string InvitedEmail,
    string? DisplayName,
    TripRole Role,
    ParticipantStatus Status,
    DateTime CreatedAt);
