using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TripFlow.Api.Authorization;
using TripFlow.Application.Abstractions;
using TripFlow.Domain.Enums;

namespace TripFlow.Api.RealTime;

/// <summary>
/// Um grupo do SignalR por viagem - so entra quem for participante aceito (checado aqui na
/// entrada do grupo, ja que Hub nao passa pelo mesmo pipeline de rota/policy dos controllers).
/// </summary>
[Authorize]
public class TripHub : Hub<ITripHubClient>
{
    private readonly IAppDbContext _db;
    private readonly TripPresenceTracker _presence;

    public TripHub(IAppDbContext db, TripPresenceTracker presence)
    {
        _db = db;
        _presence = presence;
    }

    public async Task JoinTrip(Guid tripId)
    {
        var userId = Context.User!.GetUserId();

        var participant = await _db.TripParticipants.AsNoTracking().FirstOrDefaultAsync(p =>
            p.TripId == tripId && p.UserId == userId && p.Status == ParticipantStatus.Accepted);

        if (participant is null)
            throw new HubException("Voce nao e participante dessa viagem.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tripId));

        var displayName = participant.DisplayName ?? participant.InvitedEmail;
        var online = _presence.Join(tripId, Context.ConnectionId, userId, displayName);
        await Clients.Group(GroupName(tripId)).PresenceChanged(online);
    }

    public async Task LeaveTrip(Guid tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(tripId));
        await BroadcastPresenceAfterLeaveAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await BroadcastPresenceAfterLeaveAsync();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task BroadcastPresenceAfterLeaveAsync()
    {
        if (_presence.Leave(Context.ConnectionId) is not { } result)
            return;

        await Clients.Group(GroupName(result.TripId)).PresenceChanged(result.Remaining);
    }

    public static string GroupName(Guid tripId) => $"trip-{tripId}";
}
