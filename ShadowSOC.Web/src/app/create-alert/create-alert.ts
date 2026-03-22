import { Component, inject } from '@angular/core';
import { AlertService } from '../alert';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-create-alert',
  imports: [FormsModule],
  templateUrl: './create-alert.html',
  styleUrl: './create-alert.css',
})
export class CreateAlert {
  private alertService = inject(AlertService);
  submitted = false;

  alert: any = {
    originIp: '',
    destinationIp: '',
    typeOfAttack: '',
    severity: 'Low',
    protocol: 'Tcp',
    sourcePort: 0,
    destinationPort: 0,
    mitreID: '',
    whenStarted: '',
    whenEnded: '',
  };

  submit() {
    this.alertService.createAlert(this.alert).subscribe(() => {
      this.submitted = true;
      setTimeout(() => (this.submitted = false), 3000);
    });
  }
}
