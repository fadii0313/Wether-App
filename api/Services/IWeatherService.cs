using api.Models;

namespace api.Services;

public interface IWeatherService
{
    Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default);
    Task<WeatherOverview?> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
    Task<WeatherOverview?> GetWeatherByCoordinatesAsync(double latitude, double longitude, string? cityName = null, string? country = null, CancellationToken cancellationToken = default);
}
