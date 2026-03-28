import { Injectable, NgZone, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { Alert } from './models/alert.model';

@Injectable({
  providedIn: 'root',
})
export class Signalr {
  private connection: signalR.HubConnection;
  private ngZone = inject(NgZone);
  alerts$ = new Subject<Alert>();
  allAlerts: Alert[] = [];

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
        console.log('Alert received:', alert.TypeOfAttack, alert.OriginIp);
      });
    });

    this.connection.start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error('SignalR connection error:', err));
  }
}
