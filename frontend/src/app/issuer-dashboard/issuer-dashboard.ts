import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-issuer-dashboard',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './issuer-dashboard.html',
  styleUrls: ['./issuer-dashboard.css']
})
export class IssuerDashboardComponent implements OnInit {
  requestedCount  = 0;   // Awaiting Issuer action (REQUESTED)
  issuedCount     = 0;   // Already dispatched (ISSUED)
  totalStockItems = 0;   // Active inventory items

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadStats();
  }

  loadStats() {
    // Use /api/issuer/stats (ISSUER-role-accessible endpoint)
    this.http.get<{ requestedCount: number; issuedCount: number; totalStockItems: number }>(
      `${environment.apiUrl}/issuer/stats`
    ).subscribe({
      next: stats => {
        this.requestedCount  = stats.requestedCount;
        this.issuedCount     = stats.issuedCount;
        this.totalStockItems = stats.totalStockItems;
      },
      error: err => console.error('Failed to load issuer stats:', err)
    });
  }
}
