import { Component, inject, ChangeDetectorRef, OnInit, OnDestroy } from '@angular/core';
import { AttackMap } from './attack-map/attack-map';
import { Signalr } from './signalr';
import { DatePipe } from '@angular/common';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [AttackMap, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit, OnDestroy {
  signalrService = inject(Signalr);
  private cdr = inject(ChangeDetectorRef);
  private subscription!: Subscription;

  ngOnInit() {
    this.subscription = this.signalrService.alerts$.subscribe(() => {
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  get totalAttacks(): number {
    return this.signalrService.allAlerts.length;
  }

  get criticalCount(): number {
    return this.signalrService.allAlerts.filter(a => a.Severity === 'Critical').length;
  }

  get highCount(): number {
    return this.signalrService.allAlerts.filter(a => a.Severity === 'High').length;
  }

  get mediumCount(): number {
    return this.signalrService.allAlerts.filter(a => a.Severity === 'Medium').length;
  }

  get lowCount(): number {
    return this.signalrService.allAlerts.filter(a => a.Severity === 'Low').length;
  }

  get topCountries(): { country: string; count: number }[] {
    const counts: Record<string, number> = {};
    for (const alert of this.signalrService.allAlerts) {
      const c = alert.Country || 'Unknown';
      counts[c] = (counts[c] || 0) + 1;
    }
    return Object.entries(counts)
      .map(([country, count]) => ({ country, count }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 5);
  }

  get attacksByType(): { type: string; count: number }[] {
    const counts: Record<string, number> = {};
    for (const alert of this.signalrService.allAlerts) {
      const t = alert.TypeOfAttack || 'Unknown';
      counts[t] = (counts[t] || 0) + 1;
    }
    return Object.entries(counts)
      .map(([type, count]) => ({ type, count }))
      .sort((a, b) => b.count - a.count);
  }

  get maxTypeCount(): number {
    const types = this.attacksByType;
    return types.length > 0 ? types[0].count : 1;
  }

  get maxSeverityCount(): number {
    return Math.max(this.criticalCount, this.highCount, this.mediumCount, this.lowCount, 1);
  }
}
