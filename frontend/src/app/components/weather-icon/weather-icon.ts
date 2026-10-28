import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-weather-icon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="icon-container" [attr.data-size]="size" [ngSwitch]="normalizedIcon">
      <!-- Clear Day / Sun -->
      <svg *ngSwitchCase="'clear-day'" viewBox="0 0 64 64" class="weather-svg sun-svg">
        <circle cx="32" cy="32" r="14" fill="url(#sun-grad)" class="sun-body" />
        <g stroke="url(#sun-ray-grad)" stroke-width="3" stroke-linecap="round" class="sun-rays">
          <line x1="32" y1="8" x2="32" y2="3" />
          <line x1="32" y1="56" x2="32" y2="61" />
          <line x1="8" y1="32" x2="3" y2="32" />
          <line x1="56" y1="32" x2="61" y2="32" />
          <line x1="15" y1="15" x2="11" y2="11" />
          <line x1="49" y1="49" x2="53" y2="53" />
          <line x1="15" y1="49" x2="11" y2="53" />
          <line x1="49" y1="15" x2="53" y2="11" />
        </g>
        <defs>
          <radialGradient id="sun-grad" cx="40%" cy="40%" r="60%">
            <stop offset="0%" stop-color="#FFF3B0" />
            <stop offset="60%" stop-color="#FFB703" />
            <stop offset="100%" stop-color="#FB8500" />
          </radialGradient>
          <linearGradient id="sun-ray-grad" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stop-color="#FFB703" />
            <stop offset="100%" stop-color="#FB8500" />
          </linearGradient>
        </defs>
      </svg>

      <!-- Clear Night / Moon -->
      <svg *ngSwitchCase="'clear-night'" viewBox="0 0 64 64" class="weather-svg moon-svg">
        <path d="M42 16 A 20 20 0 1 0 48 48 A 24 24 0 0 1 42 16 Z" fill="url(#moon-grad)" class="moon-body" />
        <circle cx="20" cy="18" r="1.5" fill="#FFEAA7" class="star star-1" />
        <circle cx="50" cy="22" r="2" fill="#FFEAA7" class="star star-2" />
        <circle cx="48" cy="46" r="1.5" fill="#FFEAA7" class="star star-3" />
        <defs>
          <linearGradient id="moon-grad" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stop-color="#FFFDE7" />
            <stop offset="50%" stop-color="#E0E7FF" />
            <stop offset="100%" stop-color="#818CF8" />
          </linearGradient>
        </defs>
      </svg>

      <!-- Partly Cloudy Day -->
      <svg *ngSwitchCase="'partly-cloudy-day'" viewBox="0 0 64 64" class="weather-svg">
        <circle cx="24" cy="22" r="11" fill="url(#sun-grad-mini)" class="sun-mini" />
        <path d="M22 48 h26 a10 10 0 0 0 1-20 a14 14 0 0 0 -26 -2 a9 9 0 0 0 -1 22 z" fill="url(#cloud-grad)" class="cloud-front" />
        <defs>
          <radialGradient id="sun-grad-mini" cx="35%" cy="35%" r="65%">
            <stop offset="0%" stop-color="#FFF3B0" />
            <stop offset="100%" stop-color="#FB8500" />
          </radialGradient>
          <linearGradient id="cloud-grad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stop-color="#FFFFFF" />
            <stop offset="100%" stop-color="#CBD5E1" />
          </linearGradient>
        </defs>
      </svg>

      <!-- Partly Cloudy Night -->
      <svg *ngSwitchCase="'partly-cloudy-night'" viewBox="0 0 64 64" class="weather-svg">
        <path d="M30 14 A 14 14 0 1 0 34 36 A 16 16 0 0 1 30 14 Z" fill="url(#moon-grad)" class="moon-mini" />
        <path d="M22 48 h26 a10 10 0 0 0 1-20 a14 14 0 0 0 -26 -2 a9 9 0 0 0 -1 22 z" fill="url(#cloud-grad)" class="cloud-front" />
      </svg>

      <!-- Cloudy / Overcast -->
      <svg *ngSwitchCase="'cloudy'" viewBox="0 0 64 64" class="weather-svg">
        <path d="M16 42 h28 a8 8 0 0 0 0-16 a12 12 0 0 0 -22 -2 a8 8 0 0 0 -6 18 z" fill="#94A3B8" opacity="0.6" class="cloud-back" />
        <path d="M20 50 h30 a10 10 0 0 0 0-20 a14 14 0 0 0 -26 -2 a9 9 0 0 0 -4 22 z" fill="url(#cloud-grad)" class="cloud-front" />
      </svg>

      <!-- Rain / Light Rain / Showers -->
      <svg *ngSwitchCase="'rain'" viewBox="0 0 64 64" class="weather-svg">
        <path d="M18 38 h28 a9 9 0 0 0 0-18 a13 13 0 0 0 -24 -2 a8 8 0 0 0 -4 20 z" fill="url(#cloud-rain-grad)" class="cloud-rain" />
        <g stroke="#38BDF8" stroke-width="2.5" stroke-linecap="round" class="raindrops">
          <line x1="24" y1="44" x2="21" y2="54" class="drop drop-1" />
          <line x1="33" y1="44" x2="30" y2="54" class="drop drop-2" />
          <line x1="42" y1="44" x2="39" y2="54" class="drop drop-3" />
        </g>
        <defs>
          <linearGradient id="cloud-rain-grad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stop-color="#CBD5E1" />
            <stop offset="100%" stop-color="#64748B" />
          </linearGradient>
        </defs>
      </svg>

      <!-- Thunderstorm -->
      <svg *ngSwitchCase="'thunderstorm'" viewBox="0 0 64 64" class="weather-svg">
        <path d="M18 34 h28 a9 9 0 0 0 0-18 a13 13 0 0 0 -24 -2 a8 8 0 0 0 -4 20 z" fill="#475569" class="cloud-storm" />
        <polygon points="32,34 26,44 32,44 28,56 40,42 33,42" fill="#FACC15" class="lightning" />
        <g stroke="#38BDF8" stroke-width="2" stroke-linecap="round">
          <line x1="21" y1="44" x2="19" y2="51" class="drop drop-1" />
          <line x1="43" y1="44" x2="41" y2="51" class="drop drop-3" />
        </g>
      </svg>

      <!-- Snow -->
      <svg *ngSwitchCase="'snow'" viewBox="0 0 64 64" class="weather-svg">
        <path d="M18 36 h28 a9 9 0 0 0 0-18 a13 13 0 0 0 -24 -2 a8 8 0 0 0 -4 20 z" fill="url(#cloud-grad)" />
        <g fill="#E0F2FE" class="snowflakes">
          <circle cx="23" cy="46" r="2.5" class="snow-1" />
          <circle cx="33" cy="50" r="2" class="snow-2" />
          <circle cx="43" cy="45" r="2.5" class="snow-3" />
        </g>
      </svg>

      <!-- Fog -->
      <svg *ngSwitchCase="'fog'" viewBox="0 0 64 64" class="weather-svg">
        <path d="M22 28 h24 a7 7 0 0 0 0-14 a10 10 0 0 0 -18 -1 a6 6 0 0 0 -6 15 z" fill="#CBD5E1" opacity="0.7" />
        <g stroke="#E2E8F0" stroke-width="3" stroke-linecap="round" opacity="0.85">
          <line x1="14" y1="36" x2="50" y2="36" class="fog-line-1" />
          <line x1="18" y1="44" x2="46" y2="44" class="fog-line-2" />
          <line x1="12" y1="52" x2="52" y2="52" class="fog-line-3" />
        </g>
      </svg>

      <!-- Default / Fallback -->
      <svg *ngSwitchDefault viewBox="0 0 64 64" class="weather-svg">
        <circle cx="26" cy="26" r="10" fill="#FFB703" />
        <path d="M20 48 h26 a9 9 0 0 0 0-18 a12 12 0 0 0 -22 -2 a8 8 0 0 0 -4 20 z" fill="#E2E8F0" />
      </svg>
    </div>
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
    .icon-container {
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .icon-container[data-size="sm"] svg { width: 32px; height: 32px; }
    .icon-container[data-size="md"] svg { width: 48px; height: 48px; }
    .icon-container[data-size="lg"] svg { width: 72px; height: 72px; }
    .icon-container[data-size="xl"] svg { width: 110px; height: 110px; }

    .weather-svg {
      filter: drop-shadow(0 4px 10px rgba(0, 0, 0, 0.15));
      transition: transform 0.3s ease;
    }
    :host:hover .weather-svg {
      transform: scale(1.05);
    }

    /* Animations */
    .sun-rays {
      transform-origin: 32px 32px;
      animation: rotateSun 18s linear infinite;
    }
    @keyframes rotateSun {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }

    .moon-body {
      filter: drop-shadow(0 0 12px rgba(129, 140, 248, 0.5));
    }
    .star {
      animation: twinkle 2s ease-in-out infinite alternate;
    }
    .star-2 { animation-delay: 0.7s; }
    .star-3 { animation-delay: 1.3s; }
    @keyframes twinkle {
      0% { opacity: 0.3; transform: scale(0.8); }
      100% { opacity: 1; transform: scale(1.2); }
    }

    .cloud-back {
      animation: floatCloudBack 4s ease-in-out infinite alternate;
    }
    .cloud-front {
      animation: floatCloudFront 3.5s ease-in-out infinite alternate;
    }
    @keyframes floatCloudBack {
      0% { transform: translateX(0px); }
      100% { transform: translateX(3px); }
    }
    @keyframes floatCloudFront {
      0% { transform: translateY(0px); }
      100% { transform: translateY(-2px); }
    }

    .drop {
      stroke-dasharray: 4;
      animation: fall 1.2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
    }
    .drop-2 { animation-delay: 0.3s; }
    .drop-3 { animation-delay: 0.6s; }
    @keyframes fall {
      0% { transform: translateY(-2px); opacity: 0; }
      50% { opacity: 1; }
      100% { transform: translateY(6px); opacity: 0; }
    }

    .lightning {
      animation: flash 2.5s infinite;
      filter: drop-shadow(0 0 8px #FACC15);
    }
    @keyframes flash {
      0%, 88%, 94%, 100% { opacity: 0; transform: scale(0.9); }
      90%, 93% { opacity: 1; transform: scale(1.05); }
    }

    .snowflakes circle {
      animation: snowFall 2s linear infinite;
    }
    .snow-2 { animation-delay: 0.6s; }
    .snow-3 { animation-delay: 1.2s; }
    @keyframes snowFall {
      0% { transform: translateY(-3px) translateX(0); opacity: 0; }
      50% { opacity: 1; }
      100% { transform: translateY(6px) translateX(2px); opacity: 0.2; }
    }

    .fog-line-1, .fog-line-2, .fog-line-3 {
      animation: fogShift 3s ease-in-out infinite alternate;
    }
    .fog-line-2 { animation-delay: 0.8s; }
    .fog-line-3 { animation-delay: 1.5s; }
    @keyframes fogShift {
      0% { transform: translateX(-3px); }
      100% { transform: translateX(3px); }
    }
  `]
})
export class WeatherIconComponent {
  @Input() icon = 'cloudy';
  @Input() size: 'sm' | 'md' | 'lg' | 'xl' = 'md';

  get normalizedIcon(): string {
    const raw = (this.icon || '').toLowerCase();
    if (raw.includes('clear-day') || raw === 'sunny') return 'clear-day';
    if (raw.includes('clear-night')) return 'clear-night';
    if (raw.includes('partly-cloudy-night')) return 'partly-cloudy-night';
    if (raw.includes('partly-cloudy')) return 'partly-cloudy-day';
    if (raw.includes('thunder')) return 'thunderstorm';
    if (raw.includes('snow')) return 'snow';
    if (raw.includes('rain') || raw.includes('drizzle') || raw.includes('showers')) return 'rain';
    if (raw.includes('fog')) return 'fog';
    return 'cloudy';
  }
}
