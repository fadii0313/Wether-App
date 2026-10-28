using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current weather and forecasts for a specified city name.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(WeatherOverview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherOverview>> GetOverview([FromQuery] string city = "London", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new { message = "City name cannot be empty." });
        }

        var overview = await _weatherService.GetWeatherAsync(city, cancellationToken);
        if (overview == null)
        {
            return NotFound(new { message = $"Weather data not found for '{city}'." });
        }

        return Ok(overview);
    }

    /// <summary>
    /// Gets the current weather and forecasts by latitude and longitude.
    /// </summary>
    [HttpGet("coords")]
    [ProducesResponseType(typeof(WeatherOverview), StatusCodes.Status200OK)]
    public async Task<ActionResult<WeatherOverview>> GetByCoordinates(
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] string? cityName = null,
        [FromQuery] string? country = null,
        CancellationToken cancellationToken = default)
    {
        var overview = await _weatherService.GetWeatherByCoordinatesAsync(lat, lon, cityName, country, cancellationToken);
        if (overview == null)
        {
            return NotFound(new { message = "Weather data not found for coordinates." });
        }

        return Ok(overview);
    }

    /// <summary>
    /// Searches for cities matching the search query.
    /// </summary>
    [HttpGet("cities")]
    [ProducesResponseType(typeof(IEnumerable<CityInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CityInfo>>> SearchCities([FromQuery] string? query, CancellationToken cancellationToken = default)
    {
        var cities = await _weatherService.SearchCitiesAsync(query ?? string.Empty, cancellationToken);
        return Ok(cities);
    }
}
