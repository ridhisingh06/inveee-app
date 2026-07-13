import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { RefreshService } from '../services/refresh.service';

@Component({
  selector: 'app-issuer-dashboard',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './issuer-dashboard.html',
  styleUrls: ['./issuer-dashboard.css']
})
export class IssuerDashboardComponent implements OnInit, OnDestroy {
  requestedCount  = 0;
  issuedCount     = 0;
  totalStockItems = 0;

  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private refresh: RefreshService
  ) {}

  ngOnInit() {
    // ✅ Reload stats whenever IssuerIssueComponent successfully submits a
    //    partial issue, so the dashboard cards reflect the new counts
    //    immediately without requiring a manual browser refresh.
    this.refresh.issuer$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadStats());

    this.loadStats();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadStats() {
    this.http.get<{ requestedCount: number; issuedCount: number; totalStockItems: number }>(
      `${environment.apiUrl}/issuer/stats`
    ).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: stats => {
          this.requestedCount  = stats.requestedCount;
          this.issuedCount     = stats.issuedCount;
          this.totalStockItems = stats.totalStockItems;
        },
        error: err => console.error('Failed to load issuer stats:', err)
      });
  }
}
