using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Budgets;
using TripFlow.Application.Budgets.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/budgets")]
[Authorize]
public class BudgetsController : ApiControllerBase
{
    private readonly BudgetService _budgetService;

    public BudgetsController(BudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _budgetService.ListAsync(tripId, ct));
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<BudgetDto>> Upsert(Guid tripId, [FromBody] UpsertBudgetRequest request, CancellationToken ct)
    {
        return Ok(await _budgetService.UpsertAsync(tripId, request, ct));
    }

    [HttpDelete("{budgetId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, Guid budgetId, CancellationToken ct)
    {
        var result = await _budgetService.DeleteAsync(tripId, budgetId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
