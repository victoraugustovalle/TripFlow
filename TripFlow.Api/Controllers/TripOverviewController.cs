using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Overview;
using TripFlow.Application.Overview.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/overview")]
[Authorize]
public class TripOverviewController : ApiControllerBase
{
    private readonly OverviewService _overviewService;

    public TripOverviewController(OverviewService overviewService)
    {
        _overviewService = overviewService;
    }

    /// <summary>Resumo consolidado da viagem: orcamento planejado vs. gasto por categoria, quanto o usuario autenticado deve/tem a receber, progresso do checklist e os proximos itens do roteiro a partir de hoje. Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<TripOverviewDto>> Get(Guid tripId, CancellationToken ct)
    {
        return Ok(await _overviewService.GetAsync(tripId, User.GetUserId(), ct));
    }
}
