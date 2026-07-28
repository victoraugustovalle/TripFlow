using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Trips;
using TripFlow.Application.Trips.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips")]
[Authorize]
public class TripsController : ApiControllerBase
{
    private readonly TripService _tripService;

    public TripsController(TripService tripService)
    {
        _tripService = tripService;
    }

    /// <summary>Cria uma viagem nova. Quem cria vira automaticamente participante com papel Owner.</summary>
    [HttpPost]
    public async Task<ActionResult<TripDto>> Create([FromBody] CreateTripRequest request, CancellationToken ct)
    {
        var trip = await _tripService.CreateAsync(User.GetUserId(), User.GetEmail(), User.GetName(), request, ct);
        return CreatedAtAction(nameof(GetById), new { tripId = trip.Id }, trip);
    }

    /// <summary>Lista as viagens em que o usuario autenticado e participante aceito, com o papel (Owner/Editor/Viewer) que ele tem em cada uma.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TripSummaryDto>>> ListMine(CancellationToken ct)
    {
        return Ok(await _tripService.ListForUserAsync(User.GetUserId(), ct));
    }

    /// <summary>Detalhe de uma viagem. Requer ser participante (qualquer papel) da viagem.</summary>
    [HttpGet("{tripId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<TripDto>> GetById(Guid tripId, CancellationToken ct)
    {
        return FromResult(await _tripService.GetByIdAsync(tripId, ct));
    }

    /// <summary>Atualiza dados da viagem (nome, datas, status, etc). Requer papel Editor ou Owner.</summary>
    [HttpPut("{tripId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<TripDto>> Update(Guid tripId, [FromBody] UpdateTripRequest request, CancellationToken ct)
    {
        return FromResult(await _tripService.UpdateAsync(tripId, request, ct));
    }

    /// <summary>Apaga a viagem e tudo que pertence a ela (participantes, gastos, checklist, orcamento). Requer papel Owner.</summary>
    [HttpDelete("{tripId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripOwnerPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, CancellationToken ct)
    {
        var result = await _tripService.DeleteAsync(tripId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
