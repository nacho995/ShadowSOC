import { Injectable, NgZone, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { Alert, ThreatAnalysis } from './models/alert.model';

@Injectable({
  providedIn: 'root',
})
export class Signalr {
  private connection: signalR.HubConnection;
  private ngZone = inject(NgZone);
  alerts$ = new Subject<Alert>();
  analysis$ = new Subject<ThreatAnalysis>();
  allAlerts: Alert[] = [];
  allAnalyses: ThreatAnalysis[] = [];

  constructor() {
    const apiUrl = window.location.hostname === 'localhost'
      ? 'http://localhost:5174'
      : 'https://shadowsoc-api.fly.dev';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/hubs/alerts`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('NewAlert', (message: string) => {
      this.ngZone.run(() => {
        const alert = JSON.parse(message) as Alert;
        alert.Timestamp = new Date();
        this.allAlerts.unshift(alert);
        if (this.allAlerts.length > 200) this.allAlerts.pop();
        this.alerts$.next(alert);
      });
    });

    this.connection.on('NewAnalysis', (message: string) => {
      console.log('RAW NewAnalysis:', message, typeof message);
      this.ngZone.run(() => {
        try {
          const parsed = typeof message === 'string' ? JSON.parse(message) : message;
          const analysis = (typeof parsed === 'string' ? JSON.parse(parsed) : parsed) as ThreatAnalysis;
          this.allAnalyses.unshift(analysis);
          if (this.allAnalyses.length > 50) this.allAnalyses.pop();
          this.analysis$.next(analysis);
        } catch (e) {
          console.error('Failed to parse analysis:', e);
        }
      });
    });


    this.connection.start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error('SignalR connection error:', err));
  }
}
