namespace api.Models;

public record CityInfo(
    string Name,
    string Country,
    string? State,
    double Latitude,
    double Longitude
);

public record CurrentWeather(
    string CityName,
    string Country,
    double Latitude,
    double Longitude,
    double TemperatureC,
    double TemperatureF,
    double FeelsLikeC,
    double FeelsLikeF,
    double MinTempC,
    double MaxTempC,
    string Condition,
    int WeatherCode,
    string Icon,
    int Humidity,
    double WindSpeedKmh,
    double WindSpeedMph,
    int WindDirectionDegrees,
    string WindDirectionCompass,
    double PressureHpa,
    double UvIndex,
    double VisibilityKm,
    int AirQualityIndex,
    string AirQualityLabel,
    string Sunrise,
    string Sunset,
    bool IsDay,
    DateTime UpdatedAtUtc
);

public record HourlyForecastItem(
    DateTime Time,
    string FormattedTime,
    double TemperatureC,
    double TemperatureF,
    string Condition,
    int WeatherCode,
    string Icon,
    int PrecipitationProbability,
    double WindSpeedKmh
);

public record DailyForecastItem(
    DateTime Date,
    string DayOfWeek,
    string FormattedDate,
    double TempMaxC,
    double TempMaxF,
    double TempMinC,
    double TempMinF,
    string Condition,
    int WeatherCode,
    string Icon,
    int PrecipitationProbability,
    int Humidity,
    double MaxUvIndex,
    string Sunrise,
    string Sunset
);

public record WeatherOverview(
    CurrentWeather Current,
    List<HourlyForecastItem> Hourly,
    List<DailyForecastItem> Daily
);
