import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ADMIN_NAV_GROUPS } from '../models/nav-constants';
import { environment } from '../../environments/environment';

interface SummaryData {
  totalCategories: number;
  totalItems: number;
  totalStock: number;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css']
})
export class AdminDashboardComponent implements OnInit {
  search = '';
  readonly navGroups = ADMIN_NAV_GROUPS;
  summary: SummaryData | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.fetchSummary();
  }

  fetchSummary() {
    this.http.get<SummaryData>(`${environment.apiUrl}/admin/summary`)
      .subscribe({
        next: (data) => {
          this.summary = data;
        },
        error: (err) => {
          console.error('Error fetching admin summary stats:', err);
        }
      });
  }

  clearSearch() {
    this.search = '';
  }

  get totalModules(): number {
    return this.navGroups.reduce((sum, g) => sum + g.items.length, 0);
  }

  get filteredGroups() {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.navGroups;

    return this.navGroups
      .map((g) => ({
        ...g,
        items: g.items.filter((i) => {
          const hay = `${i.label} ${i.description ?? ''} ${g.title}`.toLowerCase();
          return hay.includes(q);
        })
      }))
      .filter((g) => g.items.length > 0);
  }

  trackByLabel(_: number, item: { label: string }) {
    return item.label;
  }
}
