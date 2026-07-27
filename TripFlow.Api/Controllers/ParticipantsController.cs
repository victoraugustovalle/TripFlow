using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Participants;
using TripFlow.Application.Participants.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/participants")]
[Authorize]
public class ParticipantsController : ApiControllerBase
{
    private readonly ParticipantService _participantService;

    public ParticipantsController(ParticipantService participantService)
    {
        _participantService = participantService;
    }

    [HttpPost("invite")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ParticipantDto>> Invite(Guid tripId, [FromBody] InviteParticipantRequest request, CancellationToken ct)
    {
        return FromResult(await _participantService.InviteAsync(tripId, request, ct));
    }

    [HttpPost("accept")]
    public async Task<ActionResult<ParticipantDto>> Accept(Guid tripId, [FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        return FromResult(await _participantService.AcceptInviteAsync(tripId, User.GetUserId(), User.GetEmail(), request, ct));
    }

    [HttpPost("decline")]
    public async Task<IActionResult> Decline(Guid tripId, CancellationToken ct)
    {
        var result = await _participantService.DeclineInviteAsync(tripId, User.GetEmail(), ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ParticipantDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _participantService.ListAsync(tripId, ct));
    }

    [HttpPut("{participantId:guid}/role")]
    [Authorize(Policy = AuthorizationExtensions.TripOwnerPolicy)]
    public async Task<IActionResult> UpdateRole(Guid tripId, Guid participantId, [FromBody] UpdateParticipantRoleRequest request, CancellationToken ct)
    {
        var result = await _participantService.UpdateRoleAsync(tripId, participantId, request, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }

    [HttpDelete("{participantId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripOwnerPolicy)]
    public async Task<IActionResult> Remove(Guid tripId, Guid participantId, CancellationToken ct)
    {
        var result = await _participantService.RemoveAsync(tripId, participantId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
