import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { CityInfo, WeatherOverview } from '../models/weather.model';

@Injectable({
  providedIn: 'root'
})
export class WeatherService {
  private readonly http = inject(HttpClient);
  // Default to the .NET 10 API launch URL
  private readonly baseUrl = 'http://localhost:5296/api/weather';

  getWeatherByCity(city: string): Observable<WeatherOverview> {
    const params = new HttpParams().set('city', city);
    return this.http.get<WeatherOverview>(`${this.baseUrl}/overview`, { params }).pipe(
      catchError(error => {
        console.error('Error fetching weather by city:', error);
        return throwError(() => new Error(error.error?.message || `Failed to fetch weather for "${city}".`));
      })
    );
  }

  getWeatherByCoords(lat: number, lon: number, cityName?: string, country?: string): Observable<WeatherOverview> {
    let params = new HttpParams()
      .set('lat', lat.toString())
      .set('lon', lon.toString());

    if (cityName) {
      params = params.set('cityName', cityName);
    }
    if (country) {
      params = params.set('country', country);
    }

    return this.http.get<WeatherOverview>(`${this.baseUrl}/coords`, { params }).pipe(
      catchError(error => {
        console.error('Error fetching weather by coordinates:', error);
        return throwError(() => new Error('Failed to fetch weather for current location.'));
      })
    );
  }

  searchCities(query: string): Observable<CityInfo[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<CityInfo[]>(`${this.baseUrl}/cities`, { params }).pipe(
      catchError(error => {
        console.warn('Error searching cities:', error);
        return [];
      })
    );
  }
}
