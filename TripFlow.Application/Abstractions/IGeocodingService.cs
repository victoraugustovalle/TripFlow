namespace TripFlow.Application.Abstractions;

public record GeocodeResult(string DisplayName, double Latitude, double Longitude);

public interface IGeocodingService
{
    Task<IReadOnlyList<GeocodeResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
