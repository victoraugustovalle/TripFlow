using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Itinerary.DTOs;

namespace TripFlow.Application.Itinerary;

/// <summary>
/// Usa a latitude/longitude e a data que cada ItineraryItem ja guarda pra buscar a previsao do
/// tempo do dia (Open-Meteo) e sugerir - nunca criar sozinho - um item de checklist quando a
/// previsao pede atencao (chuva, calor ou frio). Primeira conexao real entre dois modulos que
/// hoje sao ilhas (Roteiro e Checklist): o sistema passa a antecipar, nao so guardar o que foi
/// digitado.
/// </summary>
public class ItineraryWeatherService
{
    private const int RainSuggestionThresholdPercent = 50;
    private const double HeatSuggestionThresholdC = 30;
    private const double ColdSuggestionThresholdC = 12;

    private readonly IAppDbContext _db;
    private readonly IWeatherService _weatherService;

    public ItineraryWeatherService(IAppDbContext db, IWeatherService weatherService)
    {
        _db = db;
        _weatherService = weatherService;
    }

    public async Task<IReadOnlyList<ItineraryDayWeatherDto>> GetForecastAsync(Guid tripId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // So os proximos dias com pelo menos um item que tem coordenadas - sem isso nao da pra
        // perguntar nada pro provedor de clima, e previsao de dia passado nao serve pra nada.
        var upcomingItemsWithCoordinates = await _db.ItineraryItems.AsNoTracking()
            .Where(i => i.TripId == tripId && i.ItemDate >= today && i.Latitude != null && i.Longitude != null)
            .OrderBy(i => i.ItemDate).ThenBy(i => i.StartTime)
            .ToListAsync(ct);

        if (upcomingItemsWithCoordinates.Count == 0)
            return [];

        // Um item por dia (o primeiro com coordenadas, na ordem do roteiro) representa a
        // localizacao do dia - perguntar pro provedor uma vez por item seria redundante quando
        // varios ficam na mesma cidade no mesmo dia.
        var representativeItemByDate = upcomingItemsWithCoordinates
            .GroupBy(i => i.ItemDate)
            .ToDictionary(g => g.Key, g => g.First());

        // Uma coordenada pode se repetir em dias diferentes (mesma cidade) - busca a previsao
        // uma vez por coordenada e reaproveita pra todos os dias que caem nela.
        var forecastByCoordinate = new Dictionary<(double Latitude, double Longitude), IReadOnlyList<DailyForecast>>();
        var results = new List<ItineraryDayWeatherDto>();

        foreach (var (date, item) in representativeItemByDate)
        {
            var coordinate = (Latitude: item.Latitude!.Value, Longitude: item.Longitude!.Value);
            if (!forecastByCoordinate.TryGetValue(coordinate, out var forecast))
            {
                forecast = await _weatherService.GetDailyForecastAsync(coordinate.Latitude, coordinate.Longitude, ct);
                forecastByCoordinate[coordinate] = forecast;
            }

            var dayForecast = forecast.FirstOrDefault(f => f.Date == date);
            if (dayForecast is null)
                continue;

            results.Add(new ItineraryDayWeatherDto(
                date,
                dayForecast.TemperatureMaxC,
                dayForecast.TemperatureMinC,
                dayForecast.PrecipitationProbabilityPercent,
                BuildSuggestions(dayForecast)));
        }

        return results.OrderBy(r => r.Date).ToList();
    }

    private static List<string> BuildSuggestions(DailyForecast forecast)
    {
        var suggestions = new List<string>();

        if (forecast.PrecipitationProbabilityPercent >= RainSuggestionThresholdPercent)
            suggestions.Add("Guarda-chuva");

        if (forecast.TemperatureMaxC >= HeatSuggestionThresholdC)
            suggestions.Add("Protetor solar");

        if (forecast.TemperatureMinC <= ColdSuggestionThresholdC)
            suggestions.Add("Casaco");

        return suggestions;
    }
}
