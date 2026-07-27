using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TripFlow.Application.Abstractions;

namespace TripFlow.Api.Controllers;

/// <summary>Geocoding pra usar no mapa: transforma um endereco/nome de lugar em coordenadas.</summary>
[Route("api/geocode")]
[Authorize]
[EnableRateLimiting("geocoding")]
public class GeocodingController : ApiControllerBase
{
    private readonly IGeocodingService _geocodingService;

    public GeocodingController(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    /// <summary>Busca coordenadas (lat/lng) por texto livre, ex: "Cristo Redentor, Rio de Janeiro". Devolve ate 5 resultados via Nominatim/OpenStreetMap.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GeocodeResult>>> Search([FromQuery] string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Informe o parametro 'query'." });

        return Ok(await _geocodingService.SearchAsync(query, ct));
    }
}
