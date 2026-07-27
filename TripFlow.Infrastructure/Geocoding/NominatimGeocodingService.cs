using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TripFlow.Application.Abstractions;

namespace TripFlow.Infrastructure.Geocoding;

/// <summary>
/// Nominatim (OpenStreetMap) - geocoding gratuito, sem chave de API. A politica de uso deles
/// exige no maximo 1 requisicao por segundo e um User-Agent identificando a aplicacao (o
/// HttpClient e configurado com esse header via DependencyInjection); o rate limit "geocoding"
/// na Api reforca isso no lado do servidor, com uma chave global (nao por usuario).
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GeocodeResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var url = $"search?q={Uri.EscapeDataString(query)}&format=jsonv2&limit=5&addressdetails=0";

        try
        {
            var results = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url, cancellationToken);
            if (results is null)
                return [];

            return results
                .Where(r => double.TryParse(r.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                    && double.TryParse(r.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                .Select(r => new GeocodeResult(
                    r.DisplayName,
                    double.Parse(r.Lat, CultureInfo.InvariantCulture),
                    double.Parse(r.Lon, CultureInfo.InvariantCulture)))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao consultar o Nominatim para a busca '{Query}'", query);
            return [];
        }
    }

    private class NominatimResult
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;
    }
}
