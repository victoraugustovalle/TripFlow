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

    /// <summary>Lista o orcamento por categoria: valor planejado (o que foi cadastrado aqui) vs. valor real (soma dos gastos daquela categoria). Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _budgetService.ListAsync(tripId, ct));
    }

    /// <summary>Cria ou atualiza o valor planejado de uma categoria de orcamento. Requer papel Editor ou Owner.</summary>
    [HttpPut]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<BudgetDto>> Upsert(Guid tripId, [FromBody] UpsertBudgetRequest request, CancellationToken ct)
    {
        return Ok(await _budgetService.UpsertAsync(tripId, request, ct));
    }

    /// <summary>Remove o valor planejado de uma categoria (os gastos ja lancados nao sao apagados). Requer papel Editor ou Owner.</summary>
    [HttpDelete("{budgetId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, Guid budgetId, CancellationToken ct)
    {
        var result = await _budgetService.DeleteAsync(tripId, budgetId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
