import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

import { RequestStateService, IssuedRequest, IssuedRequestItem, PaginatedRequests } from '../services/request-state.service';

@Component({
  selector: 'app-pending-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pending-approvals.html',
  styleUrls: ['./pending-approvals.css']
})
export class PendingApprovalsComponent implements OnInit, OnDestroy {
  requests: IssuedRequest[] = [];
  loading = true;
  errorMsg = '';
  successMsg = '';
  processingMap: { [itemId: number]: 'approving' | 'rejecting' } = {};
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
      .subscribe(() => this.fetchRequests(1));

    this.requestState.pendingAdminRequests$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state: PaginatedRequests) => {
        this.requests = state.data;
        this.total = state.total;
        this.totalPages = state.totalPages;
        this.currentPage = state.currentPage;
        this.loading = false;
      });

    this.fetchRequests();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchChange(value: string) {
    this.searchText = value;
    this.search$.next(value);
  }

  fetchRequests(page: number = 1) {
    this.loading = true;
    this.errorMsg = '';
    const search = this.searchText?.trim();
    this.requestState.fetchPendingAdminRequests(page, this.pageSize, search);
  }

  approve(requestId: number, requestItemId: number) {
    this.successMsg = '';
    this.errorMsg = '';
    this.processingMap[requestItemId] = 'approving';
    
    this.http.patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/approve`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} has been approved.`;
          delete this.processingMap[requestItemId];
          this.statusMap[requestItemId] = 'Approved';
          // Hide toast after 3s
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err) => {
          console.error(err);
          this.errorMsg = err?.error?.message || 'Failed to approve request.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  reject(requestId: number, requestItemId: number) {
    this.successMsg = '';
    this.errorMsg = '';
    this.processingMap[requestItemId] = 'rejecting';
    
    this.http.patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/reject`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} has been rejected.`;
          delete this.processingMap[requestItemId];
          this.statusMap[requestItemId] = 'Rejected';
          // Hide toast after 3s
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err) => {
          console.error(err);
          this.errorMsg = err?.error?.message || 'Failed to reject request.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  private findItem(requestId: number, requestItemId: number): IssuedRequestItem | null {
    const req = this.requests.find(r => r.id === requestId);
    if (!req) return null;
    return req.items.find(i => i.id === requestItemId) || null;
  }

  prevPage() { if (this.currentPage > 1) this.fetchRequests(this.currentPage - 1); }
  nextPage() { if (this.currentPage < this.totalPages) this.fetchRequests(this.currentPage + 1); }

  getTotalQty(req: IssuedRequest): number {
    return (req.items ?? []).reduce((sum, it) => sum + Number(it.quantityIssued ?? 0), 0);
  }

  getStatusLabel(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'pendingadminapproval' || s === 'issued') return 'Pending Admin Approval';
    if (s === 'approved') return 'Approved';
    if (s === 'rejected') return 'Rejected';
    return status || 'Pending Admin Approval';
  }

  // Removed private removeFromList and removeItemFromList since we are keeping items in the list
}
