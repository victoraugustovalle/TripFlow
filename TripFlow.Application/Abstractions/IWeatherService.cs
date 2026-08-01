namespace TripFlow.Application.Abstractions;

public record DailyForecast(
    DateOnly Date,
    double? TemperatureMaxC,
    double? TemperatureMinC,
    double? PrecipitationProbabilityPercent);

public interface IWeatherService
{
    /// <summary>Previsao diaria pra uma coordenada, do dia de hoje ate o limite do provedor
    /// (tipicamente uns 10 dias) - datas fora desse horizonte simplesmente nao aparecem na lista.</summary>
    Task<IReadOnlyList<DailyForecast>> GetDailyForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
