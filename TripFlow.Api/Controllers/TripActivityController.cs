using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Activity;
using TripFlow.Application.Activity.DTOs;
using TripFlow.Application.Common;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/activity")]
[Authorize]
public class TripActivityController : ApiControllerBase
{
    private readonly ActivityService _activityService;

    public TripActivityController(ActivityService activityService)
    {
        _activityService = activityService;
    }

    /// <summary>Lista a timeline de eventos da viagem (gastos, checklist, roteiro, reservas, participantes), paginada e do mais recente pro mais antigo. Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<PagedResult<ActivityLogEntryDto>>> List(Guid tripId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await _activityService.ListAsync(tripId, page, pageSize, ct));
    }
}
