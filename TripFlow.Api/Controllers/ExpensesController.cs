using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Expenses;
using TripFlow.Application.Expenses.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/expenses")]
[Authorize]
public class ExpensesController : ApiControllerBase
{
    private readonly ExpenseService _expenseService;

    public ExpensesController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ExpenseDto>> Create(Guid tripId, [FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        return FromResult(await _expenseService.CreateAsync(tripId, request, ct));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _expenseService.ListAsync(tripId, ct));
    }

    [HttpGet("settlement")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<SettlementDto>> GetSettlement(Guid tripId, CancellationToken ct)
    {
        return Ok(await _expenseService.GetSettlementAsync(tripId, ct));
    }

    [HttpDelete("{expenseId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, Guid expenseId, CancellationToken ct)
    {
        var result = await _expenseService.DeleteAsync(tripId, expenseId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
