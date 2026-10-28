# Atmosphere // Weather Studio

A modern, high-precision weather application featuring separate frontend and backend directories:
- **Backend (`/api`)**: Built with **.NET 10** Web API (`net10.0`), OpenAPI, Memory Caching, and resilient weather provider integration.
- **Frontend (`/frontend`)**: Built with **Angular** standalone components, reactive Signals, modern glassmorphic UI, dynamic weather-reactive themes, and animated SVG icons.

---

## Project Structure

```
g:/TESTCASE/
├── api/                             # .NET 10 Backend Web API
│   ├── Controllers/
│   │   └── WeatherController.cs     # Endpoints for city weather, coords, and autocomplete
│   ├── Models/
│   │   └── WeatherModels.cs         # DTOs (CurrentWeather, Hourly, Daily, CityInfo)
│   ├── Services/
│   │   ├── IWeatherService.cs       # Service contract
│   │   └── WeatherService.cs        # Open-Meteo live integration + resilient fallback generator
│   ├── Properties/
│   │   └── launchSettings.json      # Ports (HTTP: 5296, HTTPS: 7145)
│   ├── Program.cs                   # CORS policies, DI container, OpenAPI
│   └── api.csproj                   # Targets net10.0
│
└── frontend/                        # Angular Frontend
    ├── src/
    │   ├── app/
    │   │   ├── components/
    │   │   │   └── weather-icon/    # Animated vector weather icons
    │   │   ├── models/
    │   │   │   └── weather.model.ts # TypeScript interfaces matching .NET 10 contracts
    │   │   ├── services/
    │   │   │   └── weather.service.ts # HTTP client service for backend endpoints
    │   │   ├── app.ts               # Root component with Signals, Geolocation & State
    │   │   ├── app.html             # Glassmorphic UI layout
    │   │   ├── app.css              # Dynamic weather themes and micro-animations
    │   │   └── app.config.ts        # HttpClient withFetch() configuration
    │   ├── styles.css               # Design tokens and weather theme CSS variables
    │   └── index.html               # Typography & metadata
    ├── angular.json                 # Angular configuration
    └── package.json
```

---

## Quick Start Instructions

### 1. Run the .NET 10 Backend API

From the root directory:
```bash
dotnet run --project api/api.csproj --launch-profile http
```
The API starts at: `http://localhost:5296`
- OpenAPI Specification: `http://localhost:5296/openapi/v1.json`

#### Key API Endpoints:
- `GET /api/weather/overview?city=London` - Full weather overview (current, 24-hr hourly, 7-day forecast)
- `GET /api/weather/coords?lat=51.5&lon=-0.12` - Geolocation-based weather overview
- `GET /api/weather/cities?query=New` - City search with autocomplete suggestions

### 2. Run the Angular Frontend

From the `frontend` directory:
```bash
cd frontend
npm start
```
The frontend starts at: `http://localhost:4200`

---

## Features

- **Live & Resilient Weather**: Connects to Open-Meteo for real-time global weather data with zero API key configuration needed, backed by an algorithmic fallback generator if offline.
- **Dynamic Weather Themes**: The interface background and ambient glow adapt dynamically to current weather conditions (Clear Day, Clear Night, Rain, Thunderstorm, Snow, Cloudy).
- **Interactive Metrics Grid**: Real-time readings for Wind with compass azimuth, Relative Humidity with level indicator, UV Index with protection badges, Air Quality Index (AQI), Barometric Pressure, and Sunrise/Sunset cycles.
- **24-Hour Hourly Timeline**: Smooth horizontal scroller displaying temperature curves, weather icons, and precipitation probabilities.
- **7-Day Extended Forecast**: Daily outlook with temperature visual range bars and rain chance badges.
- **Unit Toggle (°C / °F)**: Instant switching between Celsius and Fahrenheit.
- **Location Detection & Favorites**: Geolocation API integration and customizable favorite city chips saved to `localStorage`.
