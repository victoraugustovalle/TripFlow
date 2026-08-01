using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Journal;
using TripFlow.Application.Journal.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/me")]
[Authorize]
public class MeController : ApiControllerBase
{
    private readonly JournalService _journalService;

    public MeController(JournalService journalService)
    {
        _journalService = journalService;
    }

    /// <summary>Todas as viagens concluidas de que o usuario autenticado participou, com o
    /// resumo de cada uma (gasto total, nota media) - a linha do tempo pessoal de viagens,
    /// atravessando a particao por Trip que domina o resto da API.</summary>
    [HttpGet("journal")]
    public async Task<ActionResult<MyJournalDto>> GetJournal(CancellationToken ct)
    {
        return Ok(await _journalService.GetMyJournalAsync(User.GetUserId(), ct));
    }
}
