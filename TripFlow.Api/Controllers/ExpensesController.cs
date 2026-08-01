using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Common;
using TripFlow.Application.Expenses;
using TripFlow.Application.Expenses.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/expenses")]
[Authorize]
public class ExpensesController : ApiControllerBase
{
    private readonly ExpenseService _expenseService;
    private readonly SettlementService _settlementService;

    public ExpensesController(ExpenseService expenseService, SettlementService settlementService)
    {
        _expenseService = expenseService;
        _settlementService = settlementService;
    }

    /// <summary>Lanca um gasto na viagem. Sem informar a divisao, divide igualmente entre todos os participantes aceitos (o resto de centavos por arredondamento vai pros primeiros da lista). Requer papel Editor ou Owner.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ExpenseDto>> Create(Guid tripId, [FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        return FromResult(await _expenseService.CreateAsync(tripId, User.GetUserId(), request, ct));
    }

    /// <summary>Lista os gastos da viagem, paginado e do mais recente pro mais antigo. Aceita filtro por categoria exata, periodo (from/to) e busca livre (casa com descricao ou categoria). Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> List(Guid tripId, [FromQuery] ExpenseListQuery query, CancellationToken ct)
    {
        return Ok(await _expenseService.ListAsync(tripId, query, ct));
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
        var result = await _expenseService.DeleteAsync(tripId, User.GetUserId(), expenseId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }

    /// <summary>Marca uma transferencia sugerida pelo settlement como paga (fora do app - Pix, dinheiro...). O devedor e sempre o usuario autenticado. Fica pendente ate quem recebeu confirmar. Requer ser participante da viagem.</summary>
    [HttpPost("settlement/mark-paid")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<SettlementRecordDto>> MarkSettlementPaid(Guid tripId, [FromBody] MarkSettlementPaidRequest request, CancellationToken ct)
    {
        return FromResult(await _settlementService.MarkAsPaidAsync(tripId, User.GetUserId(), request, ct));
    }

    /// <summary>Confirma o recebimento de uma quitacao marcada como paga - so quem recebeu (o credor) pode confirmar. So depois disso o debito some do saldo. Requer ser participante da viagem.</summary>
    [HttpPost("settlement/{recordId:guid}/confirm")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<SettlementRecordDto>> ConfirmSettlement(Guid tripId, Guid recordId, CancellationToken ct)
    {
        return FromResult(await _settlementService.ConfirmAsync(tripId, User.GetUserId(), recordId, ct));
    }
}
