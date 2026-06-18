import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  RequestStateService,
  IssuedRequest,
  PaginatedRequests
} from '../services/request-state.service';
import { normalizeStatus, getStatusLabel } from '../utils/status.util';

@Component({
  selector: 'app-pending-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pending-approvals.html',
  styleUrls: ['./pending-approvals.css']
})
export class PendingApprovalsComponent implements OnInit, OnDestroy {
  requests:     IssuedRequest[] = [];
  loading        = true;
  errorMsg       = '';
  successMsg     = '';
  processingMap: { [itemId: number]: 'approving' | 'rejecting' } = {};
  statusMap:     { [itemId: number]: string }                    = {};

  searchText  = '';
  currentPage = 1;
  pageSize    = 10;
  total       = 0;
  totalPages  = 0;

  private readonly search$  = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly http:         HttpClient,
    private readonly requestState: RequestStateService
  ) {}

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => this.fetchRequests(1));

    this.requestState.pendingAdminRequests$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state: PaginatedRequests) => {
        this.requests    = state.data;
        this.total       = state.total;
        this.totalPages  = state.totalPages;
        this.currentPage = state.currentPage;
        this.loading     = false;
      });

    this.fetchRequests();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchChange(value: string): void {
    this.searchText = value;
    this.search$.next(value);
  }

  fetchRequests(page = 1): void {
    this.loading  = true;
    this.errorMsg = '';
    this.requestState.fetchPendingAdminRequests(page, this.pageSize, this.searchText.trim());
  }

  approve(requestId: number, requestItemId: number): void {
    this.processingMap[requestItemId] = 'approving';
    this.successMsg = '';
    this.errorMsg   = '';

    this.http
      .patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/approve`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} approved.`;
          delete this.processingMap[requestItemId];
          this.statusMap[requestItemId] = 'approved';
          this.requestState.updateItemStatus('ADMIN', requestId, requestItemId, 'Approved');
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message ?? 'Failed to approve item.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  reject(requestId: number, requestItemId: number): void {
    this.processingMap[requestItemId] = 'rejecting';
    this.successMsg = '';
    this.errorMsg   = '';

    this.http
      .patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/reject`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} rejected.`;
          delete this.processingMap[requestItemId];
          this.statusMap[requestItemId] = 'rejected';
          this.requestState.updateItemStatus('ADMIN', requestId, requestItemId, 'Rejected');
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message ?? 'Failed to reject item.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  prevPage(): void { if (this.currentPage > 1)              this.fetchRequests(this.currentPage - 1); }
  nextPage(): void { if (this.currentPage < this.totalPages) this.fetchRequests(this.currentPage + 1); }

  getTotalQty(req: IssuedRequest): number {
    // At PendingAdminApproval stage quantityRequested is available; fall back to quantityIssued
    return (req.items ?? []).reduce(
      (sum, it) => sum + Number((it as any).quantityRequested ?? it.quantityIssued ?? 0), 0
    );
  }

  // ── Template helpers (delegates to shared util) ─────────────────────────

  normalizeStatus(status: string | null | undefined): string {
    return normalizeStatus(status);
  }

  getStatusLabel(status: string | null | undefined): string {
    return getStatusLabel(status);
  }
}
