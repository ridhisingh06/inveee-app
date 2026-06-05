import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { normalizeStatus, getStatusLabel } from '../utils/status.util';

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

  // ── Template helpers (delegates to shared util) ─────────────────────────

  normalizeStatus(status: string | null | undefined): string {
    return normalizeStatus(status);
  }

  getItemStatusLabel(status: string | null | undefined): string {
    return getStatusLabel(status);
  }
}
