using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Retrospective;
using TripFlow.Application.Retrospective.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/retrospective")]
[Authorize]
public class RetrospectiveController : ApiControllerBase
{
    private readonly RetrospectiveService _retrospectiveService;
    private readonly TripMemoryService _tripMemoryService;

    public RetrospectiveController(RetrospectiveService retrospectiveService, TripMemoryService tripMemoryService)
    {
        _retrospectiveService = retrospectiveService;
        _tripMemoryService = tripMemoryService;
    }

    /// <summary>Resumo retrospectivo da viagem: total gasto vs. planejado, ranking de quem pagou mais, progresso do checklist, contagem do roteiro e as memorias (melhor momento/nota/foto) que os participantes registraram. Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<TripRetrospectiveDto>> Get(Guid tripId, CancellationToken ct)
    {
        var result = await _retrospectiveService.GetAsync(tripId, ct);
        return result is null ? NotFound(new { message = "Viagem nao encontrada." }) : Ok(result);
    }

    /// <summary>Cria ou atualiza a sua propria memoria da viagem (melhor momento, nota de 1 a 5, foto) - sempre em nome de quem esta autenticado. Requer ser participante da viagem.</summary>
    [HttpPut("memory")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<TripMemoryDto>> UpsertMemory(Guid tripId, [FromBody] UpsertTripMemoryRequest request, CancellationToken ct)
    {
        return FromResult(await _tripMemoryService.UpsertAsync(tripId, User.GetUserId(), request, ct));
    }
}
