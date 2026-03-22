import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class AlertService {
  private http = inject(HttpClient);

  createAlert(alert: any){
    return this.http.post('http://localhost:5174/api/alerts', alert);
  }
}
