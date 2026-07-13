import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { WorkflowService } from '../services/workflow.service';
import { RefreshService } from '../services/refresh.service';
import { getStatusClass } from '../utils/status.util';
import { OrderHistoryList, OrderHistoryItem } from '../models/request.model';

/**
 * OrderHistoryComponent — My Orders Page
 *
 * Subscribes to RefreshService.orders$ so that when a user confirms receipt
 * in UserCheckStatusComponent the order history table refreshes immediately
 * without requiring a manual browser reload.
 */
@Component({
  standalone: true,
  selector: 'app-order-history',
  imports: [CommonModule, FormsModule],
  templateUrl: './order-history.html',
  styleUrls: ['./order-history.css']
})
export class OrderHistoryComponent implements OnInit, OnDestroy {
  orders: OrderHistoryItem[] = [];
  loading       = true;
  errorMsg      = '';

  // ── Pagination ─────────────────────────────────────────────────────────────
  currentPage = 1;
  pageSize    = 12;
  totalCount  = 0;

  // ── Search / filter ────────────────────────────────────────────────────────
  searchText = '';
  statusFilter: string | null = null;
  sortBy: 'date' | 'status' = 'date';

  private readonly search$  = new Subject<string>();
  private readonly destroy$ = new Subject<void>();
  getStatusClass = getStatusClass;

  constructor(
    private readonly workflow: WorkflowService,
    private readonly router: Router,
    private readonly refresh: RefreshService
  ) {}

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => this.loadOrders(1));

    // ✅ Reload order history whenever a receipt is confirmed elsewhere
    //    (e.g. UserCheckStatusComponent.receiveAll) so this list shows the
    //    new order without a manual browser refresh.
    this.refresh.orders$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadOrders(this.currentPage));

    this.loadOrders(1);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  loadOrders(page: number): void {
    this.loading     = true;
    this.errorMsg    = '';
    this.currentPage = page;

    this.workflow.getOrderHistory(page, this.pageSize)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result: OrderHistoryList) => {
          this.totalCount = result.totalCount;
          this.orders     = this.applyClientFilters(result.orders);
          this.loading    = false;
        },
        error: (err) => {
          this.errorMsg = err.message || 'Failed to load order history.';
          this.loading  = false;
        }
      });
  }

  onSearchChange(value: string): void {
    this.searchText = value;
    this.search$.next(value);
  }

  // ── Client-side filtering / sorting ───────────────────────────────────────

  applyClientFilters(orders: OrderHistoryItem[]): OrderHistoryItem[] {
    let filtered = [...orders];

    const normalizedStatusFilter = (this.statusFilter ?? '').toLowerCase();
    if (normalizedStatusFilter) {
      filtered = filtered.filter(o => (o.status ?? '').toLowerCase() === normalizedStatusFilter);
    }

    const normalizedSearchText = (this.searchText ?? '').trim().toLowerCase();
    if (normalizedSearchText) {
      filtered = filtered.filter(o =>
        o.requestId.toString().includes(normalizedSearchText) ||
        (o.status ?? '').toLowerCase().includes(normalizedSearchText)
      );
    }

    if (this.sortBy === 'date') {
      filtered.sort((a, b) => new Date(b.receivedDate).getTime() - new Date(a.receivedDate).getTime());
    } else if (this.sortBy === 'status') {
      filtered.sort((a, b) => a.status.localeCompare(b.status));
    }

    return filtered;
  }

  setStatusFilter(status: string | null): void {
    this.statusFilter = status;
    this.loadOrders(1);
  }

  setSortBy(sort: 'date' | 'status'): void {
    this.sortBy = sort;
    this.orders = this.applyClientFilters(this.orders);
  }

  // ── Navigation ────────────────────────────────────────────────────────────

  viewSummary(orderSummaryId: number): void {
    this.router.navigate(['/user-dashboard/order-summary', orderSummaryId]);
  }

  // ── Pagination ─────────────────────────────────────────────────────────────

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }
  prevPage(): void { if (this.currentPage > 1) this.loadOrders(this.currentPage - 1); }
  nextPage(): void { if (this.currentPage < this.totalPages) this.loadOrders(this.currentPage + 1); }
}
