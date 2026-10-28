export interface CityInfo {
  name: string;
  country: string;
  state?: string | null;
  latitude: number;
  longitude: number;
}

export interface CurrentWeather {
  cityName: string;
  country: string;
  latitude: number;
  longitude: number;
  temperatureC: number;
  temperatureF: number;
  feelsLikeC: number;
  feelsLikeF: number;
  minTempC: number;
  maxTempC: number;
  condition: string;
  weatherCode: number;
  icon: string;
  humidity: number;
  windSpeedKmh: number;
  windSpeedMph: number;
  windDirectionDegrees: number;
  windDirectionCompass: string;
  pressureHpa: number;
  uvIndex: number;
  visibilityKm: number;
  airQualityIndex: number;
  airQualityLabel: string;
  sunrise: string;
  sunset: string;
  isDay: boolean;
  updatedAtUtc: string;
}

export interface HourlyForecastItem {
  time: string;
  formattedTime: string;
  temperatureC: number;
  temperatureF: number;
  condition: string;
  weatherCode: number;
  icon: string;
  precipitationProbability: number;
  windSpeedKmh: number;
}

export interface DailyForecastItem {
  date: string;
  dayOfWeek: string;
  formattedDate: string;
  tempMaxC: number;
  tempMaxF: number;
  tempMinC: number;
  tempMinF: number;
  condition: string;
  weatherCode: number;
  icon: string;
  precipitationProbability: number;
  humidity: number;
  maxUvIndex: number;
  sunrise: string;
  sunset: string;
}

export interface WeatherOverview {
  current: CurrentWeather;
  hourly: HourlyForecastItem[];
  daily: DailyForecastItem[];
}

export type TempUnit = 'C' | 'F';
