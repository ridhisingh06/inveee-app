import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-user-check-status',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-check-status.html',
  styleUrls: ['./user-check-status.css']
})
export class UserCheckStatusComponent implements OnInit {
  requests: any[] = [];
  loading = true;
  errorMsg = '';
  successMsg = '';

  /** Tracks in-flight per-item receive calls: itemId → true */
  receivingMap: { [itemId: number]: boolean } = {};

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadRequests(); }

  loadRequests() {
    this.loading = true;
    this.errorMsg = '';
    this.http.get<any>(`${environment.apiUrl}/requests`)
      .subscribe({
        next: res => {
          this.requests = Array.isArray(res) ? res : (res.data ?? []);
          this.loading = false;
        },
        error: () => {
          this.errorMsg = 'Could not fetch your requests. Please try again.';
          this.loading = false;
        }
      });
  }

  // ── Per-item receive ──────────────────────────────────────────────────────

  receiveItem(requestId: number, itemId: number) {
    this.receivingMap[itemId] = true;
    this.successMsg = '';
    this.errorMsg = '';

    this.http.patch(`${environment.apiUrl}/requests/${requestId}/items/${itemId}/receive`, {})
      .subscribe({
        next: () => {
          const req = this.requests.find((r: any) => r.id === requestId);
          if (req?.items) {
            const item = req.items.find((i: any) => i.id === itemId);
            if (item) item.status = 'Received';

            const allTerminal = req.items.every((i: any) => {
              const s = this.normalizeStatus(i.status);
              return s === 'received' || s === 'notissued' || s === 'rejected';
            });
            if (allTerminal) req.status = 'Received';
          }
          this.successMsg = 'Item marked as received.';
          delete this.receivingMap[itemId];
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: err => {
          this.errorMsg = err?.error?.message || 'Failed to mark item as received.';
          delete this.receivingMap[itemId];
        }
      });
  }

  // ── Status helpers (request level) ───────────────────────────────────────

  getStatusClass(status: string): string {
    const s = this.normalizeStatus(status);
    if (s === 'pendingwithissuer') return 'status-requested';
    if (s === 'pendingadminapproval') return 'status-issued';
    if (s === 'notissued') return 'status-not-issued';
    if (s === 'approved') return 'status-approved';
    if (s === 'rejected') return 'status-rejected';
    if (s === 'received') return 'status-received';
    return 'status-pending';
  }

  getStatusIcon(status: string): string {
    const s = this.normalizeStatus(status);
    if (s === 'pendingwithissuer') return '1';
    if (s === 'notissued') return '!';
    if (s === 'pendingadminapproval') return '2';
    if (s === 'approved') return '3';
    if (s === 'rejected') return 'x';
    if (s === 'received') return '✓';
    return '-';
  }

  getStatusLabel(status: string): string {
    const s = this.normalizeStatus(status);
    if (s === 'pendingwithissuer') return 'Pending with Issuer';
    if (s === 'notissued') return 'Not Issued';
    if (s === 'pendingadminapproval') return 'Pending Admin Approval';
    if (s === 'approved') return 'Approved';
    if (s === 'rejected') return 'Rejected';
    if (s === 'received') return 'Received';
    return status || 'Pending';
  }

  // ── Status helpers (item level) ───────────────────────────────────────────

  getItemStatusClass(status: string): string { return this.getStatusClass(status); }
  getItemStatusLabel(status: string): string { return this.getStatusLabel(status); }
  isItemApproved(status: string): boolean { return this.normalizeStatus(status) === 'approved'; }
  isItemReceived(status: string): boolean { return this.normalizeStatus(status) === 'received'; }

  // ── Counters ──────────────────────────────────────────────────────────────

  get pendingCount() {
    return this.requests.filter(r => {
      const s = this.normalizeStatus(r.status);
      return s === 'pendingwithissuer' || s === 'pendingadminapproval';
    }).length;
  }
  get requestedCount() { return this.requests.filter(r => this.normalizeStatus(r.status) === 'pendingwithissuer').length; }
  get issuedCount()    { return this.requests.filter(r => this.normalizeStatus(r.status) === 'pendingadminapproval').length; }
  get approvedCount()  { return this.requests.filter(r => this.normalizeStatus(r.status) === 'approved').length; }
  get rejectedCount()  {
    return this.requests.filter(r => {
      const s = this.normalizeStatus(r.status);
      return s === 'rejected' || s === 'notissued';
    }).length;
  }

  private normalizeStatus(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'requested') return 'pendingwithissuer';
    if (s === 'issued') return 'pendingadminapproval';
    return s;
  }
}
