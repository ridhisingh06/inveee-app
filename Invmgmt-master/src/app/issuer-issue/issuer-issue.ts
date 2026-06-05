import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  RequestStateService,
  PaginatedRequests
} from '../services/request-state.service';
import { normalizeStatus, getStatusLabel, getStatusClass } from '../utils/status.util';

@Component({
  standalone: true,
  selector: 'app-issuer-issue',
  imports: [CommonModule, FormsModule],
  templateUrl: './issuer-issue.html',
  styleUrls: ['./issuer-issue.css']
})
export class IssuerIssueComponent implements OnInit, OnDestroy {
  requests:      any[]   = [];
  loading        = true;
  errorMsg       = '';
  successMsg     = '';
  processingMap: { [itemId: number]: 'issuing' | 'rejecting' } = {};
  statusMap:     { [itemId: number]: string }                  = {};

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
      .subscribe(() => this.loadRequests(1));

    this.requestState.pendingIssuerRequests$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state: PaginatedRequests) => {
        this.requests    = state.data;
        this.total       = state.total;
        this.totalPages  = state.totalPages;
        this.currentPage = state.currentPage;
        this.loading     = false;
      });

    this.loadRequests();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchChange(value: string): void {
    this.searchText = value;
    this.search$.next(value);
  }

  loadRequests(page = 1): void {
    this.loading  = true;
    this.errorMsg = '';
    this.requestState.fetchPendingIssuerRequests(page, this.pageSize, this.searchText.trim());
  }

  issue(requestId: number, requestItemId: number): void {
    this.processingMap[requestItemId] = 'issuing';
    this.successMsg = '';
    this.errorMsg   = '';

    this.http
      .patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/issue`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} issued — pending admin approval.`;
          delete this.processingMap[requestItemId];
          // Store normalised key so template comparisons work
          this.statusMap[requestItemId] = 'pendingadminapproval';
          this.requestState.updateItemStatus('ISSUER', requestId, requestItemId, 'PendingAdminApproval');
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message ?? 'Failed to issue item.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  reject(requestId: number, requestItemId: number): void {
    if (!confirm(`Mark item #${requestItemId} as not issued?`)) return;

    this.processingMap[requestItemId] = 'rejecting';
    this.successMsg = '';
    this.errorMsg   = '';

    this.http
      .patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/not-issue`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} marked as not issued.`;
          delete this.processingMap[requestItemId];
          this.statusMap[requestItemId] = 'notissued';
          this.requestState.updateItemStatus('ISSUER', requestId, requestItemId, 'NotIssued');
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message ?? 'Failed to mark item as not issued.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  prevPage(): void { if (this.currentPage > 1)              this.loadRequests(this.currentPage - 1); }
  nextPage(): void { if (this.currentPage < this.totalPages) this.loadRequests(this.currentPage + 1); }

  getTotalQty(req: any): number {
    return (req?.items ?? []).reduce(
      (sum: number, it: any) => sum + Number(it?.quantityRequested ?? 0), 0
    );
  }

  // ── Template helpers (delegates to shared util) ─────────────────────────

  normalizeStatus(status: string | null | undefined): string {
    return normalizeStatus(status);
  }

  getStatusLabel(status: string | null | undefined): string {
    return getStatusLabel(status);
  }

  getStatusClass(status: string | null | undefined): string {
    return getStatusClass(status);
  }
}
