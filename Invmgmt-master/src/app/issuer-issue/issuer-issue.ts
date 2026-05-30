import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { RequestStateService, IssuedRequest, IssuedRequestItem, PaginatedRequests } from '../services/request-state.service';

@Component({
  standalone: true,
  selector: 'app-issuer-issue',
  imports: [CommonModule, FormsModule],
  templateUrl: './issuer-issue.html',
  styleUrls: ['./issuer-issue.css']
})
export class IssuerIssueComponent implements OnInit, OnDestroy {
  requests: any[] = [];
  loading = true;
  errorMsg = '';
  successMsg = '';
  processingMap: { [itemId: number]: 'issuing' | 'rejecting' } = {};
  statusMap: { [itemId: number]: string } = {};

  searchText = '';
  private search$ = new Subject<string>();
  private destroy$ = new Subject<void>();

  currentPage = 1;
  pageSize = 10;
  total = 0;
  totalPages = 0;

  constructor(private http: HttpClient, private requestState: RequestStateService) {}

  ngOnInit() {
    this.search$
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => this.loadRequests(1));

    this.requestState.pendingIssuerRequests$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state: PaginatedRequests) => {
        this.requests = state.data;
        this.total = state.total;
        this.totalPages = state.totalPages;
        this.currentPage = state.currentPage;
        this.loading = false;
      });

    this.loadRequests();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchChange(value: string) {
    this.searchText = value;
    this.search$.next(value);
  }

  loadRequests(page: number = 1) {
    this.loading = true;
    this.errorMsg = '';
    const search = this.searchText?.trim();
    this.requestState.fetchPendingIssuerRequests(page, this.pageSize, search);
  }

  issue(requestId: number, requestItemId: number) {
    this.processingMap[requestItemId] = 'issuing';
    this.successMsg = '';
    this.errorMsg = '';

    this.http.patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/issue`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} issued and sent for admin approval.`;
          delete this.processingMap[requestItemId];
          this.statusMap[requestItemId] = 'PendingAdminApproval';
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: err => {
          this.errorMsg = err?.error?.message || 'Failed to issue request.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  reject(requestId: number, requestItemId: number) {
    if (!confirm(`Mark item #${requestItemId} as not issued?`)) return;

    this.processingMap[requestItemId] = 'rejecting';
    this.successMsg = '';
    this.errorMsg = '';

    this.http.patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/not-issue`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} marked as not issued.`;
          delete this.processingMap[requestItemId];
          this.statusMap[requestItemId] = 'NotIssued';
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: err => {
          this.errorMsg = err?.error?.message || 'Failed to mark request as not issued.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  private findItem(requestId: number, requestItemId: number): IssuedRequestItem | null {
    const req = this.requests.find(r => r.id === requestId);
    if (!req) return null;
    return req.items.find((i: any) => i.id === requestItemId) || null;
  }

  // Removed removeFromList and removeItemFromList since we want to keep items visible

  prevPage() { if (this.currentPage > 1) this.loadRequests(this.currentPage - 1); }
  nextPage() { if (this.currentPage < this.totalPages) this.loadRequests(this.currentPage + 1); }

  getTotalQty(req: any): number {
    const items = req?.items ?? [];
    return items.reduce((sum: number, it: any) => sum + Number(it?.quantityRequested ?? 0), 0);
  }

  getStatusClass(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'pendingwithissuer' || s === 'requested') return 'badge requested';
    if (s === 'pendingadminapproval' || s === 'issued') return 'badge issued';
    if (s === 'notissued' || s === 'rejected') return 'badge rejected';
    return 'badge';
  }

  getStatusLabel(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'pendingwithissuer' || s === 'requested') return 'Pending with Issuer';
    if (s === 'pendingadminapproval' || s === 'issued') return 'Pending Admin Approval';
    if (s === 'notissued') return 'Not Issued';
    if (s === 'rejected') return 'Rejected';
    return status || 'Pending';
  }
}
