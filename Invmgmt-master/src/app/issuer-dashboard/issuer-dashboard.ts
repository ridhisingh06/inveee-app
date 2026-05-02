import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar';

@Component({
  selector: 'app-issuer-dashboard',
  standalone: true,
  imports: [NavbarComponent, RouterModule],
  templateUrl: './issuer-dashboard.html',
  styleUrls: ['./issuer-dashboard.css']
})
export class IssuerDashboardComponent {}
