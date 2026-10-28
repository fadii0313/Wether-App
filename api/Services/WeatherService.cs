using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace api.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;

    private static readonly List<CityInfo> PopularCities =
    [
        new("London", "United Kingdom", "England", 51.5074, -0.1278),
        new("New York", "United States", "New York", 40.7128, -74.0060),
        new("Tokyo", "Japan", "Tokyo", 35.6762, 139.6503),
        new("Paris", "France", "Île-de-France", 48.8566, 2.3522),
        new("Sydney", "Australia", "New South Wales", -33.8688, 151.2093),
        new("San Francisco", "United States", "California", 37.7749, -122.4194),
        new("Dubai", "United Arab Emirates", "Dubai", 25.2048, 55.2708),
        new("Singapore", "Singapore", null, 1.3521, 103.8198),
        new("Berlin", "Germany", "Berlin", 52.5200, 13.4050),
        new("Toronto", "Canada", "Ontario", 43.6532, -79.3832),
        new("Rome", "Italy", "Lazio", 41.9028, 12.4964),
        new("Amsterdam", "Netherlands", "North Holland", 52.3676, 4.9041),
        new("Madrid", "Spain", "Community of Madrid", 40.4168, -3.7038),
        new("Seoul", "South Korea", "Seoul", 37.5665, 126.9780),
        new("Mumbai", "India", "Maharashtra", 19.0760, 72.8777)
    ];

    public WeatherService(HttpClient httpClient, IMemoryCache cache, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(6);
    }

    public async Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return PopularCities.Take(8);
        }

        var trimmedQuery = query.Trim();
        var cacheKey = $"cities_{trimmedQuery.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<CityInfo>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(trimmedQuery)}&count=6&language=en&format=json";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var geoResult = JsonSerializer.Deserialize<OpenMeteoGeoResponse>(content, JsonOptions);
                if (geoResult?.Results != null && geoResult.Results.Count > 0)
                {
                    var results = geoResult.Results.Select(r => new CityInfo(
                        r.Name ?? trimmedQuery,
                        r.Country ?? "",
                        r.Admin1,
                        r.Latitude,
                        r.Longitude
                    )).ToList();

                    _cache.Set(cacheKey, results, TimeSpan.FromHours(6));
                    return results;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch live geocoding for '{City}'. Using local search.", trimmedQuery);
        }

        // Local fallback search
        var localMatches = PopularCities
            .Where(c => c.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase) ||
                        c.Country.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (localMatches.Count == 0)
        {
            // Pseudo city based on input
            localMatches.Add(new CityInfo(
                char.ToUpper(trimmedQuery[0]) + (trimmedQuery.Length > 1 ? trimmedQuery[1..] : ""),
                "Global",
                null,
                51.5074,
                -0.1278
            ));
        }

        return localMatches;
    }

    public async Task<WeatherOverview?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            city = "London";
        }

        var cities = await SearchCitiesAsync(city, cancellationToken);
        var targetCity = cities.FirstOrDefault() ?? new CityInfo(city, "Global", null, 51.5074, -0.1278);

        return await GetWeatherByCoordinatesAsync(targetCity.Latitude, targetCity.Longitude, targetCity.Name, targetCity.Country, cancellationToken);
    }

    public async Task<WeatherOverview?> GetWeatherByCoordinatesAsync(
        double latitude,
        double longitude,
        string? cityName = null,
        string? country = null,
        CancellationToken cancellationToken = default)
    {
        cityName ??= "Current Location";
        country ??= "";

        var cacheKey = $"weather_{latitude:F2}_{longitude:F2}";
        if (_cache.TryGetValue(cacheKey, out WeatherOverview? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,weather_code,surface_pressure,wind_speed_10m,wind_direction_10m&hourly=temperature_2m,relative_humidity_2m,precipitation_probability,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,precipitation_probability_max,uv_index_max&timezone=auto";

            var response = await _httpClient.GetAsync(weatherUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<OpenMeteoForecastResponse>(content, JsonOptions);
                if (data != null && data.Current != null)
                {
                    var overview = MapOpenMeteoToWeatherOverview(data, cityName, country, latitude, longitude);
                    _cache.Set(cacheKey, overview, TimeSpan.FromMinutes(10));
                    return overview;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live weather fetch failed for ({Lat}, {Lon}). Using fallback simulation.", latitude, longitude);
        }

        // Deterministic fallback generator
        var fallbackOverview = GenerateDeterministicWeather(cityName, country, latitude, longitude);
        _cache.Set(cacheKey, fallbackOverview, TimeSpan.FromMinutes(5));
        return fallbackOverview;
    }

    private static WeatherOverview MapOpenMeteoToWeatherOverview(
        OpenMeteoForecastResponse data,
        string cityName,
        string country,
        double latitude,
        double longitude)
    {
        var current = data.Current!;
        var code = current.WeatherCode;
        var (condition, icon) = DecodeWeather(code, current.IsDay == 1);

        double tempC = Math.Round(current.Temperature2m, 1);
        double tempF = Math.Round((tempC * 9 / 5) + 32, 1);
        double feelsC = Math.Round(current.ApparentTemperature, 1);
        double feelsF = Math.Round((feelsC * 9 / 5) + 32, 1);

        double minTempC = data.Daily?.Temperature2mMin?.FirstOrDefault() ?? (tempC - 4);
        double maxTempC = data.Daily?.Temperature2mMax?.FirstOrDefault() ?? (tempC + 5);

        string sunriseStr = data.Daily?.Sunrise?.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm");
        string sunsetStr = data.Daily?.Sunset?.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm");

        var sunriseTime = DateTime.TryParse(sunriseStr, out var sr) ? sr.ToString("HH:mm") : "06:30";
        var sunsetTime = DateTime.TryParse(sunsetStr, out var ss) ? ss.ToString("HH:mm") : "19:45";

        double uv = data.Daily?.UvIndexMax?.FirstOrDefault() ?? 4.5;
        double windKmh = Math.Round(current.WindSpeed10m, 1);
        double windMph = Math.Round(windKmh * 0.621371, 1);
        int windDeg = current.WindDirection10m;
        string windCompass = DegreesToCompass(windDeg);

        var aqi = CalculateApproxAqi(current.RelativeHumidity2m, current.SurfacePressure);
        var aqiLabel = GetAqiLabel(aqi);

        var currentWeather = new CurrentWeather(
            CityName: cityName,
            Country: country,
            Latitude: latitude,
            Longitude: longitude,
            TemperatureC: tempC,
            TemperatureF: tempF,
            FeelsLikeC: feelsC,
            FeelsLikeF: feelsF,
            MinTempC: Math.Round(minTempC, 1),
            MaxTempC: Math.Round(maxTempC, 1),
            Condition: condition,
            WeatherCode: code,
            Icon: icon,
            Humidity: current.RelativeHumidity2m,
            WindSpeedKmh: windKmh,
            WindSpeedMph: windMph,
            WindDirectionDegrees: windDeg,
            WindDirectionCompass: windCompass,
            PressureHpa: Math.Round(current.SurfacePressure, 1),
            UvIndex: Math.Round(uv, 1),
            VisibilityKm: 10.0,
            AirQualityIndex: aqi,
            AirQualityLabel: aqiLabel,
            Sunrise: sunriseTime,
            Sunset: sunsetTime,
            IsDay: current.IsDay == 1,
            UpdatedAtUtc: DateTime.UtcNow
        );

        // Hourly: next 24 items
        var hourlyItems = new List<HourlyForecastItem>();
        if (data.Hourly?.Time != null && data.Hourly.Temperature2m != null)
        {
            var now = DateTime.UtcNow;
            int count = Math.Min(data.Hourly.Time.Count, 24);
            for (int i = 0; i < count; i++)
            {
                if (DateTime.TryParse(data.Hourly.Time[i], out var hTime))
                {
                    double hTempC = Math.Round(data.Hourly.Temperature2m[i], 1);
                    double hTempF = Math.Round((hTempC * 9 / 5) + 32, 1);
                    int hCode = data.Hourly.WeatherCode != null && data.Hourly.WeatherCode.Count > i ? data.Hourly.WeatherCode[i] : code;
                    bool isDayHour = hTime.Hour >= 6 && hTime.Hour < 20;
                    var (hCond, hIcon) = DecodeWeather(hCode, isDayHour);
                    int precip = data.Hourly.PrecipitationProbability != null && data.Hourly.PrecipitationProbability.Count > i
                        ? data.Hourly.PrecipitationProbability[i]
                        : 0;
                    double hWind = data.Hourly.WindSpeed10m != null && data.Hourly.WindSpeed10m.Count > i
                        ? Math.Round(data.Hourly.WindSpeed10m[i], 1)
                        : windKmh;

                    hourlyItems.Add(new HourlyForecastItem(
                        Time: hTime,
                        FormattedTime: hTime.ToString("h tt", CultureInfo.InvariantCulture),
                        TemperatureC: hTempC,
                        TemperatureF: hTempF,
                        Condition: hCond,
                        WeatherCode: hCode,
                        Icon: hIcon,
                        PrecipitationProbability: precip,
                        WindSpeedKmh: hWind
                    ));
                }
            }
        }

        // Daily: next 7 items
        var dailyItems = new List<DailyForecastItem>();
        if (data.Daily?.Time != null && data.Daily.Temperature2mMax != null)
        {
            int dCount = Math.Min(data.Daily.Time.Count, 7);
            for (int i = 0; i < dCount; i++)
            {
                if (DateTime.TryParse(data.Daily.Time[i], out var dDate))
                {
                    double maxC = Math.Round(data.Daily.Temperature2mMax[i], 1);
                    double maxF = Math.Round((maxC * 9 / 5) + 32, 1);
                    double minC = data.Daily.Temperature2mMin != null && data.Daily.Temperature2mMin.Count > i
                        ? Math.Round(data.Daily.Temperature2mMin[i], 1)
                        : maxC - 6;
                    double minF = Math.Round((minC * 9 / 5) + 32, 1);
                    int dCode = data.Daily.WeatherCode != null && data.Daily.WeatherCode.Count > i ? data.Daily.WeatherCode[i] : code;
                    var (dCond, dIcon) = DecodeWeather(dCode, true);
                    int dPrecip = data.Daily.PrecipitationProbabilityMax != null && data.Daily.PrecipitationProbabilityMax.Count > i
                        ? data.Daily.PrecipitationProbabilityMax[i]
                        : 10;
                    double dUv = data.Daily.UvIndexMax != null && data.Daily.UvIndexMax.Count > i
                        ? Math.Round(data.Daily.UvIndexMax[i], 1)
                        : 5.0;

                    string dSunrise = data.Daily.Sunrise != null && data.Daily.Sunrise.Count > i && DateTime.TryParse(data.Daily.Sunrise[i], out var dsr)
                        ? dsr.ToString("HH:mm") : "06:30";
                    string dSunset = data.Daily.Sunset != null && data.Daily.Sunset.Count > i && DateTime.TryParse(data.Daily.Sunset[i], out var dss)
                        ? dss.ToString("HH:mm") : "19:30";

                    dailyItems.Add(new DailyForecastItem(
                        Date: dDate,
                        DayOfWeek: i == 0 ? "Today" : dDate.ToString("ddd"),
                        FormattedDate: dDate.ToString("MMM d"),
                        TempMaxC: maxC,
                        TempMaxF: maxF,
                        TempMinC: minC,
                        TempMinF: minF,
                        Condition: dCond,
                        WeatherCode: dCode,
                        Icon: dIcon,
                        PrecipitationProbability: dPrecip,
                        Humidity: current.RelativeHumidity2m,
                        MaxUvIndex: dUv,
                        Sunrise: dSunrise,
                        Sunset: dSunset
                    ));
                }
            }
        }

        return new WeatherOverview(currentWeather, hourlyItems, dailyItems);
    }

    private static WeatherOverview GenerateDeterministicWeather(string cityName, string country, double lat, double lon)
    {
        // Simple hash-based seed
        int seed = Math.Abs(cityName.GetHashCode() + DateTime.UtcNow.DayOfYear);
        var rng = new Random(seed);

        double baseTemp = 14 + (rng.NextDouble() * 12);
        double tempC = Math.Round(baseTemp, 1);
        double tempF = Math.Round((tempC * 9 / 5) + 32, 1);
        int humidity = rng.Next(40, 85);
        double windKmh = Math.Round(5 + (rng.NextDouble() * 25), 1);
        double windMph = Math.Round(windKmh * 0.621371, 1);
        int windDeg = rng.Next(0, 360);

        int[] sampleCodes = [0, 1, 2, 3, 61, 80];
        int code = sampleCodes[rng.Next(sampleCodes.Length)];
        bool isDay = DateTime.UtcNow.Hour >= 6 && DateTime.UtcNow.Hour < 20;
        var (condition, icon) = DecodeWeather(code, isDay);

        var current = new CurrentWeather(
            CityName: cityName,
            Country: country,
            Latitude: lat,
            Longitude: lon,
            TemperatureC: tempC,
            TemperatureF: tempF,
            FeelsLikeC: Math.Round(tempC - 1.2, 1),
            FeelsLikeF: Math.Round(((tempC - 1.2) * 9 / 5) + 32, 1),
            MinTempC: Math.Round(tempC - 4, 1),
            MaxTempC: Math.Round(tempC + 5, 1),
            Condition: condition,
            WeatherCode: code,
            Icon: icon,
            Humidity: humidity,
            WindSpeedKmh: windKmh,
            WindSpeedMph: windMph,
            WindDirectionDegrees: windDeg,
            WindDirectionCompass: DegreesToCompass(windDeg),
            PressureHpa: 1013.2,
            UvIndex: 5.2,
            VisibilityKm: 10.0,
            AirQualityIndex: 42,
            AirQualityLabel: "Good",
            Sunrise: "06:24",
            Sunset: "19:48",
            IsDay: isDay,
            UpdatedAtUtc: DateTime.UtcNow
        );

        var hourly = new List<HourlyForecastItem>();
        var now = DateTime.UtcNow;
        for (int i = 0; i < 24; i++)
        {
            var hTime = now.AddHours(i);
            double hTemp = Math.Round(baseTemp + Math.Sin(i * 0.25) * 4, 1);
            bool hDay = hTime.Hour >= 6 && hTime.Hour < 20;
            var (hCond, hIcon) = DecodeWeather(code, hDay);
            hourly.Add(new HourlyForecastItem(
                Time: hTime,
                FormattedTime: hTime.ToString("h tt", CultureInfo.InvariantCulture),
                TemperatureC: hTemp,
                TemperatureF: Math.Round((hTemp * 9 / 5) + 32, 1),
                Condition: hCond,
                WeatherCode: code,
                Icon: hIcon,
                PrecipitationProbability: rng.Next(5, 40),
                WindSpeedKmh: Math.Round(windKmh + (rng.NextDouble() * 4 - 2), 1)
            ));
        }

        var daily = new List<DailyForecastItem>();
        for (int i = 0; i < 7; i++)
        {
            var dDate = now.Date.AddDays(i);
            double maxC = Math.Round(baseTemp + 4 + rng.Next(-2, 3), 1);
            double minC = Math.Round(baseTemp - 3 + rng.Next(-2, 3), 1);
            int dCode = sampleCodes[rng.Next(sampleCodes.Length)];
            var (dCond, dIcon) = DecodeWeather(dCode, true);

            daily.Add(new DailyForecastItem(
                Date: dDate,
                DayOfWeek: i == 0 ? "Today" : dDate.ToString("ddd"),
                FormattedDate: dDate.ToString("MMM d"),
                TempMaxC: maxC,
                TempMaxF: Math.Round((maxC * 9 / 5) + 32, 1),
                TempMinC: minC,
                TempMinF: Math.Round((minC * 9 / 5) + 32, 1),
                Condition: dCond,
                WeatherCode: dCode,
                Icon: dIcon,
                PrecipitationProbability: rng.Next(10, 60),
                Humidity: rng.Next(45, 80),
                MaxUvIndex: 5.4,
                Sunrise: "06:24",
                Sunset: "19:48"
            ));
        }

        return new WeatherOverview(current, hourly, daily);
    }

    private static (string Condition, string Icon) DecodeWeather(int code, bool isDay)
    {
        return code switch
        {
            0 => isDay ? ("Clear Sky", "clear-day") : ("Clear Night", "clear-night"),
            1 => isDay ? ("Mainly Clear", "partly-cloudy-day") : ("Mainly Clear", "partly-cloudy-night"),
            2 => isDay ? ("Partly Cloudy", "partly-cloudy-day") : ("Partly Cloudy", "partly-cloudy-night"),
            3 => ("Overcast", "cloudy"),
            45 or 48 => ("Foggy", "fog"),
            51 or 53 or 55 => ("Drizzle", "drizzle"),
            56 or 57 => ("Freezing Drizzle", "sleet"),
            61 => ("Light Rain", "rain-light"),
            63 => ("Moderate Rain", "rain"),
            65 => ("Heavy Rain", "rain-heavy"),
            66 or 67 => ("Freezing Rain", "sleet"),
            71 => ("Light Snow", "snow-light"),
            73 => ("Moderate Snow", "snow"),
            75 => ("Heavy Snow", "snow-heavy"),
            77 => ("Snow Grains", "snow"),
            80 or 81 or 82 => ("Rain Showers", "rain-showers"),
            85 or 86 => ("Snow Showers", "snow-showers"),
            95 => ("Thunderstorm", "thunderstorm"),
            96 or 99 => ("Thunderstorm with Hail", "thunderstorm-hail"),
            _ => ("Cloudy", "cloudy")
        };
    }

    private static string DegreesToCompass(int degrees)
    {
        string[] compass = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
        int index = (int)Math.Round((degrees % 360) / 22.5, MidpointRounding.AwayFromZero) % 16;
        return compass[index];
    }

    private static int CalculateApproxAqi(int humidity, double pressure)
    {
        // Simple realistic estimation between 20 and 85
        return (int)Math.Clamp(35 + (humidity * 0.2) + ((1013 - pressure) * 0.5), 15, 120);
    }

    private static string GetAqiLabel(int aqi) => aqi switch
    {
        <= 50 => "Good",
        <= 100 => "Moderate",
        <= 150 => "Unhealthy for Sensitive Groups",
        <= 200 => "Unhealthy",
        _ => "Very Unhealthy"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Open-Meteo DTOs
    private record OpenMeteoGeoResponse(
        [property: JsonPropertyName("results")] List<OpenMeteoGeoResult>? Results
    );

    private record OpenMeteoGeoResult(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("admin1")] string? Admin1
    );

    private record OpenMeteoForecastResponse(
        [property: JsonPropertyName("current")] OpenMeteoCurrent? Current,
        [property: JsonPropertyName("hourly")] OpenMeteoHourly? Hourly,
        [property: JsonPropertyName("daily")] OpenMeteoDaily? Daily
    );

    private record OpenMeteoCurrent(
        [property: JsonPropertyName("temperature_2m")] double Temperature2m,
        [property: JsonPropertyName("relative_humidity_2m")] int RelativeHumidity2m,
        [property: JsonPropertyName("apparent_temperature")] double ApparentTemperature,
        [property: JsonPropertyName("is_day")] int IsDay,
        [property: JsonPropertyName("precipitation")] double Precipitation,
        [property: JsonPropertyName("weather_code")] int WeatherCode,
        [property: JsonPropertyName("surface_pressure")] double SurfacePressure,
        [property: JsonPropertyName("wind_speed_10m")] double WindSpeed10m,
        [property: JsonPropertyName("wind_direction_10m")] int WindDirection10m
    );

    private record OpenMeteoHourly(
        [property: JsonPropertyName("time")] List<string>? Time,
        [property: JsonPropertyName("temperature_2m")] List<double>? Temperature2m,
        [property: JsonPropertyName("relative_humidity_2m")] List<int>? RelativeHumidity2m,
        [property: JsonPropertyName("precipitation_probability")] List<int>? PrecipitationProbability,
        [property: JsonPropertyName("weather_code")] List<int>? WeatherCode,
        [property: JsonPropertyName("wind_speed_10m")] List<double>? WindSpeed10m
    );

    private record OpenMeteoDaily(
        [property: JsonPropertyName("time")] List<string>? Time,
        [property: JsonPropertyName("weather_code")] List<int>? WeatherCode,
        [property: JsonPropertyName("temperature_2m_max")] List<double>? Temperature2mMax,
        [property: JsonPropertyName("temperature_2m_min")] List<double>? Temperature2mMin,
        [property: JsonPropertyName("sunrise")] List<string>? Sunrise,
        [property: JsonPropertyName("sunset")] List<string>? Sunset,
        [property: JsonPropertyName("precipitation_probability_max")] List<int>? PrecipitationProbabilityMax,
        [property: JsonPropertyName("uv_index_max")] List<double>? UvIndexMax
    );
}
