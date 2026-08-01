using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TripFlow.Application.Abstractions;

namespace TripFlow.Infrastructure.Weather;

/// <summary>
/// Open-Meteo - previsao do tempo gratuita, sem chave de API (mesmo espirito do Nominatim pro
/// geocoding). Sem HttpClient dedicado nao daria pra responder "vai chover no dia 3 do roteiro?"
/// usando a latitude/longitude/data que o ItineraryItem ja guarda.
/// </summary>
public class OpenMeteoWeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenMeteoWeatherService> _logger;

    public OpenMeteoWeatherService(HttpClient httpClient, ILogger<OpenMeteoWeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DailyForecast>> GetDailyForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lng = longitude.ToString(CultureInfo.InvariantCulture);
        var url = "v1/forecast" +
                  $"?latitude={lat}&longitude={lng}" +
                  "&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
                  "&timezone=auto&forecast_days=10";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken);
            if (response?.Daily is not { } daily)
                return [];

            var forecasts = new List<DailyForecast>();
            for (var i = 0; i < daily.Time.Count; i++)
            {
                if (!DateOnly.TryParse(daily.Time[i], CultureInfo.InvariantCulture, out var date))
                    continue;

                forecasts.Add(new DailyForecast(
                    date,
                    i < daily.TemperatureMax.Count ? daily.TemperatureMax[i] : null,
                    i < daily.TemperatureMin.Count ? daily.TemperatureMin[i] : null,
                    i < daily.PrecipitationProbabilityMax.Count ? daily.PrecipitationProbabilityMax[i] : null));
            }

            return forecasts;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Falha ao consultar o Open-Meteo para {Latitude},{Longitude}", latitude, longitude);
            return [];
        }
    }

    private class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public DailyBlock? Daily { get; set; }
    }

    private class DailyBlock
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; } = [];

        [JsonPropertyName("temperature_2m_max")]
        public List<double?> TemperatureMax { get; set; } = [];

        [JsonPropertyName("temperature_2m_min")]
        public List<double?> TemperatureMin { get; set; } = [];

        [JsonPropertyName("precipitation_probability_max")]
        public List<double?> PrecipitationProbabilityMax { get; set; } = [];
    }
}
