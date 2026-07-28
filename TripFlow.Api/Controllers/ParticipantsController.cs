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

    /// <summary>Convida alguem por e-mail pra participar da viagem, com um papel (Viewer/Editor/Owner). Requer papel Editor ou Owner. O token do convite e enviado por e-mail (ou logado).</summary>
    [HttpPost("invite")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ParticipantDto>> Invite(Guid tripId, [FromBody] InviteParticipantRequest request, CancellationToken ct)
    {
        return FromResult(await _participantService.InviteAsync(tripId, request, ct));
    }

    /// <summary>Aceita um convite usando o token recebido, vinculando o usuario autenticado ao convite (precisa ser o mesmo e-mail convidado).</summary>
    [HttpPost("accept")]
    public async Task<ActionResult<ParticipantDto>> Accept(Guid tripId, [FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        return FromResult(await _participantService.AcceptInviteAsync(tripId, User.GetUserId(), User.GetEmail(), User.GetName(), request, ct));
    }

    /// <summary>Recusa o convite pendente do usuario autenticado pra essa viagem.</summary>
    [HttpPost("decline")]
    public async Task<IActionResult> Decline(Guid tripId, CancellationToken ct)
    {
        var result = await _participantService.DeclineInviteAsync(tripId, User.GetEmail(), ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }

    /// <summary>Lista todos os participantes da viagem (convidados, aceitos e recusados). Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ParticipantDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _participantService.ListAsync(tripId, ct));
    }

    /// <summary>Muda o papel de um participante (Viewer/Editor/Owner). Requer papel Owner. Nao permite rebaixar o ultimo Owner da viagem.</summary>
    [HttpPut("{participantId:guid}/role")]
    [Authorize(Policy = AuthorizationExtensions.TripOwnerPolicy)]
    public async Task<IActionResult> UpdateRole(Guid tripId, Guid participantId, [FromBody] UpdateParticipantRoleRequest request, CancellationToken ct)
    {
        var result = await _participantService.UpdateRoleAsync(tripId, participantId, request, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }

    /// <summary>Remove um participante da viagem. Requer papel Owner. Nao permite remover o ultimo Owner.</summary>
    [HttpDelete("{participantId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripOwnerPolicy)]
    public async Task<IActionResult> Remove(Guid tripId, Guid participantId, CancellationToken ct)
    {
        var result = await _participantService.RemoveAsync(tripId, participantId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
