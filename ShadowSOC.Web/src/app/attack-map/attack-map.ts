import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { Signalr } from '../signalr';
import { Subscription } from 'rxjs';
import * as L from 'leaflet';

@Component({
  selector: 'app-attack-map',
  imports: [],
  templateUrl: './attack-map.html',
  styleUrl: './attack-map.css',
})
export class AttackMap implements OnInit, OnDestroy {
  private signalrService = inject(Signalr);
  private map!: L.Map;
  private subscription!: Subscription;

  ngOnInit() {
    this.map = L.map('map', {
      zoomControl: false,
      attributionControl: false,
      minZoom: 2,
      maxZoom: 6,
      maxBounds: L.latLngBounds(L.latLng(-85, -180), L.latLng(85, 180)),
      maxBoundsViscosity: 1.0,
    }).setView([20, 0], 2);

    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
      attribution: '&copy; OpenStreetMap',
      noWrap: false,
    }).addTo(this.map);

    this.subscription = this.signalrService.alerts$.subscribe((alert) => {
      if (alert.Latitude != null && alert.Longitude != null && (alert.Latitude !== 0 || alert.Longitude !== 0)) {
        const color = this.getColor(alert.Severity);
        const pos: L.LatLngExpression = [alert.Latitude, alert.Longitude];

        this.dropRipple(pos, color);

        L.circleMarker(pos, {
          radius: 4,
          color: color,
          fillColor: color,
          fillOpacity: 0.7,
          weight: 0,
        })
        .bindPopup(
          `<div style="font-family: monospace; font-size: 12px; color: #ccc; background: #111; padding: 8px; border: 1px solid ${color}; border-radius: 4px;">
            <div style="color: ${color}; font-weight: bold; margin-bottom: 4px;">${alert.TypeOfAttack}</div>
            <div>IP: ${alert.OriginIp}</div>
            <div>Country: ${alert.Country}</div>
            <div>Severity: <span style="color: ${color};">${alert.Severity}</span></div>
          </div>`,
          { className: 'dark-popup' }
        )
        .addTo(this.map);
      }
    });
  }

  dropRipple(pos: L.LatLngExpression, color: string) {
    for (let i = 0; i < 3; i++) {
      setTimeout(() => {
        const frames = 20;
        const maxRadius = 25 + i * 8;
        let frame = 0;

        const ring = L.circleMarker(pos, {
          radius: 3,
          color: color,
          fillColor: 'transparent',
          fillOpacity: 0,
          weight: 2,
          opacity: 0.8,
        }).addTo(this.map);

        const interval = setInterval(() => {
          frame++;
          const progress = frame / frames;
          const radius = 3 + progress * maxRadius;
          const opacity = 0.8 * (1 - progress);

          ring.setRadius(radius);
          ring.setStyle({ opacity: opacity, weight: Math.max(1, 2 * (1 - progress)) });

          if (frame >= frames) {
            clearInterval(interval);
            if (this.map.hasLayer(ring)) {
              this.map.removeLayer(ring);
            }
          }
        }, 50);
      }, i * 300);
    }
  }

  private getColor(severity: string): string {
    switch (severity) {
      case 'Critical': return '#ff0000';
      case 'High': return '#ff4400';
      case 'Medium': return '#ffaa00';
      default: return '#ffff00';
    }
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }
}
