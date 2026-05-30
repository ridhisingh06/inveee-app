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
    if (s === 'received') return '4';
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

  get pendingCount() {
    return this.requests.filter(r => {
      const status = this.normalizeStatus(r.status);
      return status === 'pendingwithissuer' || status === 'pendingadminapproval';
    }).length;
  }

  get requestedCount() {
    return this.requests.filter(r => this.normalizeStatus(r.status) === 'pendingwithissuer').length;
  }

  get issuedCount() {
    return this.requests.filter(r => this.normalizeStatus(r.status) === 'pendingadminapproval').length;
  }

  get approvedCount() {
    return this.requests.filter(r => this.normalizeStatus(r.status) === 'approved').length;
  }

  get rejectedCount() {
    return this.requests.filter(r => {
      const status = this.normalizeStatus(r.status);
      return status === 'rejected' || status === 'notissued';
    }).length;
  }

  private normalizeStatus(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'requested') return 'pendingwithissuer';
    if (s === 'issued') return 'pendingadminapproval';
    return s;
  }
}
