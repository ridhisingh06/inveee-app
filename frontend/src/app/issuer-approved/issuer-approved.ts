import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { normalizeStatus, getStatusLabel, getStatusClass } from '../utils/status.util';

@Component({
  standalone: true,
  selector: 'app-issuer-approved',
  imports: [CommonModule],
  templateUrl: './issuer-approved.html',
  styleUrls: ['./issuer-approved.css']
})
export class IssuerApprovedComponent implements OnInit {
  requests: any[] = [];
  loading = true;
  errorMsg   = '';
  successMsg = '';
  normalizeStatus = normalizeStatus;
  getStatusLabel = getStatusLabel;
  getStatusClass = getStatusClass;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadApproved();
  }

  loadApproved(): void {
    this.loading   = true;
    this.errorMsg  = '';
    // ISSUER role is permitted to query status=Approved via GET /api/requests
    this.http.get<any>(`${environment.apiUrl}/requests?status=Approved`)
      .subscribe({
        next: res => {
          this.requests = Array.isArray(res) ? res : (res.data ?? []);
          this.loading  = false;
        },
        error: () => {
          this.errorMsg = 'Failed to load approved requests.';
          this.loading  = false;
        }
      });
  }

  /**
   * Legacy "Dispatch Items" button kept for templates that still reference it.
   * For this workflow the items are already approved — no further issuer action
   * is needed. The button now just refreshes the list to reflect the latest state.
   */
  issueRequest(_id: number): void {
    this.loadApproved();
  }

  // Template helpers delegated to shared util via bound properties

  getItemStatusLabel(status: any): string {
    return this.getStatusLabel ? this.getStatusLabel(status) : getStatusLabel(status);
  }
}

// Backwards-compatible helper used by some templates
// Keeps the public API stable for templates that expect `getItemStatusLabel`.
export function __issuerApproved_getItemStatusLabel_fallback(status: any) {
  return getStatusLabel(status);
}

