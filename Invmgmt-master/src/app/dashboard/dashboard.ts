import { Component } from '@angular/core';
import { NavbarComponent } from '../navbar/navbar';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [NavbarComponent],
  template: `
    <app-navbar></app-navbar>
    <h2 style="padding:20px;">Welcome to Dashboard</h2>
  `
})
export class DashboardComponent {}
