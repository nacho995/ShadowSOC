import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Alert } from './models/alert.model';

@Injectable({
  providedIn: 'root',
})
export class AlertService {
  private http = inject(HttpClient);

  createAlert(alert: Alert){
    return this.http.post('http://localhost:5174/api/alerts', alert);
  }
}
