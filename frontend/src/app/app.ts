import { Component, OnInit, signal, computed, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WeatherService } from './services/weather.service';
import { WeatherOverview, CityInfo, TempUnit } from './models/weather.model';
import { WeatherIconComponent } from './components/weather-icon/weather-icon';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, WeatherIconComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly weatherService = inject(WeatherService);
  private readonly searchSubject = new Subject<string>();

  // Reactive state using Angular Signals
  readonly weather = signal<WeatherOverview | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly searchQuery = signal<string>('');
  readonly citySuggestions = signal<CityInfo[]>([]);
  readonly showSuggestions = signal<boolean>(false);
  readonly isSearching = signal<boolean>(false);
  readonly tempUnit = signal<TempUnit>('C');
  readonly favorites = signal<string[]>(['London', 'Tokyo', 'New York', 'Paris', 'Sydney']);

  readonly quickPills = ['London', 'New York', 'Tokyo', 'Paris', 'Dubai', 'Sydney', 'Singapore', 'Berlin'];

  // Computed properties
  readonly isCurrentFavorite = computed(() => {
    const currentCity = this.weather()?.current.cityName;
    if (!currentCity) return false;
    return this.favorites().some(f => f.toLowerCase() === currentCity.toLowerCase());
  });

  readonly weatherTheme = computed(() => {
    const icon = this.weather()?.current.icon || 'clear-day';
    if (icon.includes('clear-night') || icon.includes('partly-cloudy-night')) return 'clear-night';
    if (icon.includes('clear-day') || icon === 'sunny') return 'clear-day';
    if (icon.includes('thunder')) return 'thunderstorm';
    if (icon.includes('snow')) return 'snow';
    if (icon.includes('rain') || icon.includes('drizzle')) return 'rain';
    return 'cloudy';
  });

  ngOnInit(): void {
    // Load saved favorites from local storage if available
    this.loadFavoritesFromStorage();

    // Setup debounced search autocomplete
    this.searchSubject.pipe(
      debounceTime(280),
      distinctUntilChanged()
    ).subscribe(query => {
      this.executeCitySearch(query);
    });

    // Initial weather load
    this.fetchWeather('London');
  }

  fetchWeather(city: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.showSuggestions.set(false);

    this.weatherService.getWeatherByCity(city).subscribe({
      next: data => {
        this.weather.set(data);
        this.isLoading.set(false);
        this.updateBodyTheme(data.current.icon);
      },
      error: err => {
        this.errorMessage.set(err.message || `Could not load weather for "${city}". Please try again.`);
        this.isLoading.set(false);
      }
    });
  }

  fetchWeatherByCoords(lat: number, lon: number): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.showSuggestions.set(false);

    this.weatherService.getWeatherByCoords(lat, lon).subscribe({
      next: data => {
        this.weather.set(data);
        this.isLoading.set(false);
        this.updateBodyTheme(data.current.icon);
      },
      error: err => {
        this.errorMessage.set(err.message || 'Could not fetch weather for your location.');
        this.isLoading.set(false);
      }
    });
  }

  useCurrentLocation(): void {
    if (!navigator.geolocation) {
      this.errorMessage.set('Geolocation is not supported by your browser.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    navigator.geolocation.getCurrentPosition(
      pos => {
        this.fetchWeatherByCoords(pos.coords.latitude, pos.coords.longitude);
      },
      err => {
        console.warn('Geolocation error:', err);
        this.errorMessage.set('Could not access current location. Please search for a city instead.');
        this.isLoading.set(false);
      },
      { timeout: 8000 }
    );
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
    if (!value || value.trim().length < 2) {
      this.citySuggestions.set([]);
      this.showSuggestions.set(false);
      return;
    }
    this.isSearching.set(true);
    this.searchSubject.next(value.trim());
  }

  onSearchSubmit(): void {
    const query = this.searchQuery().trim();
    if (query) {
      this.fetchWeather(query);
      this.searchQuery.set('');
      this.showSuggestions.set(false);
    }
  }

  selectSuggestion(city: CityInfo): void {
    this.searchQuery.set('');
    this.showSuggestions.set(false);
    this.fetchWeatherByCoords(city.latitude, city.longitude);
  }

  toggleTempUnit(): void {
    this.tempUnit.update(u => (u === 'C' ? 'F' : 'C'));
  }

  toggleFavorite(): void {
    const current = this.weather()?.current.cityName;
    if (!current) return;

    this.favorites.update(list => {
      const exists = list.some(c => c.toLowerCase() === current.toLowerCase());
      const updated = exists
        ? list.filter(c => c.toLowerCase() !== current.toLowerCase())
        : [current, ...list.slice(0, 9)];
      this.saveFavoritesToStorage(updated);
      return updated;
    });
  }

  removeFavorite(city: string, event: MouseEvent): void {
    event.stopPropagation();
    this.favorites.update(list => {
      const updated = list.filter(c => c.toLowerCase() !== city.toLowerCase());
      this.saveFavoritesToStorage(updated);
      return updated;
    });
  }

  // Close suggestions on outside click
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.search-box-wrapper')) {
      this.showSuggestions.set(false);
    }
  }

  private executeCitySearch(query: string): void {
    this.weatherService.searchCities(query).subscribe({
      next: results => {
        this.citySuggestions.set(results);
        this.showSuggestions.set(results.length > 0);
        this.isSearching.set(false);
      },
      error: () => {
        this.isSearching.set(false);
      }
    });
  }

  private updateBodyTheme(icon: string): void {
    let theme = 'cloudy';
    if (icon.includes('clear-night') || icon.includes('partly-cloudy-night')) theme = 'clear-night';
    else if (icon.includes('clear-day') || icon === 'sunny') theme = 'clear-day';
    else if (icon.includes('thunder')) theme = 'thunderstorm';
    else if (icon.includes('snow')) theme = 'snow';
    else if (icon.includes('rain') || icon.includes('drizzle')) theme = 'rain';

    document.body.setAttribute('data-weather', theme);
  }

  private loadFavoritesFromStorage(): void {
    try {
      const saved = localStorage.getItem('atmosphere_favorites');
      if (saved) {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed) && parsed.length > 0) {
          this.favorites.set(parsed);
        }
      }
    } catch (e) {
      console.warn('Could not read favorites from localStorage', e);
    }
  }

  private saveFavoritesToStorage(favorites: string[]): void {
    try {
      localStorage.setItem('atmosphere_favorites', JSON.stringify(favorites));
    } catch (e) {
      console.warn('Could not save favorites to localStorage', e);
    }
  }
}
