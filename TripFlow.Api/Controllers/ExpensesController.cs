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

    /// <summary>Lanca um gasto na viagem. Sem informar a divisao, divide igualmente entre todos os participantes aceitos (o resto de centavos por arredondamento vai pros primeiros da lista). Requer papel Editor ou Owner.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ExpenseDto>> Create(Guid tripId, [FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        return FromResult(await _expenseService.CreateAsync(tripId, request, ct));
    }

    /// <summary>Lista os gastos da viagem, do mais recente pro mais antigo. Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _expenseService.ListAsync(tripId, ct));
    }

    /// <summary>Calcula quem deve quanto pra quem, ja simplificado no menor numero de transferencias possivel. Requer ser participante da viagem.</summary>
    [HttpGet("settlement")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<SettlementDto>> GetSettlement(Guid tripId, CancellationToken ct)
    {
        return Ok(await _expenseService.GetSettlementAsync(tripId, ct));
    }

    /// <summary>Apaga um gasto (e a divisao associada a ele). Requer papel Editor ou Owner.</summary>
    [HttpDelete("{expenseId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, Guid expenseId, CancellationToken ct)
    {
        var result = await _expenseService.DeleteAsync(tripId, expenseId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
