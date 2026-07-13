import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { WorkflowService } from '../services/workflow.service';
import { CartService } from '../services/cart.service';
import { RefreshService } from '../services/refresh.service';
import { normalizeStatus, getStatusClass, getStatusLabel } from '../utils/status.util';
import { ReorderModalComponent } from './reorder-modal.component';

@Component({
  selector: 'app-user-check-status',
  standalone: true,
  imports: [CommonModule, RouterModule, ReorderModalComponent],
  templateUrl: './user-check-status.html',
  styleUrls: ['./user-check-status.css']
})
export class UserCheckStatusComponent implements OnInit, OnDestroy {
  normalizeStatus = normalizeStatus;
  getStatusClass = getStatusClass;
  getStatusLabel = getStatusLabel;
  requests: any[] = [];
  loading  = true;
  errorMsg = '';
  successMsg = '';

  /** requestId → true while "Receive All" call is in flight */
  receivingMap: { [requestId: number]: boolean } = {};

  /** requestId → orderSummaryId once received */
  orderSummaryMap: { [requestId: number]: number } = {};

  // Reorder Modal State
  isReorderModalOpen = false;
  reorderSuggestions: any[] = [];
  reorderLoading = false;

  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private workflow: WorkflowService,
    private cart: CartService,
    private router: Router,
    private refresh: RefreshService
  ) {}

  ngOnInit() {
    // ✅ Subscribe to the requests refresh signal.
    // EditRequestComponent emits this after a successful save so this list
    // refreshes automatically — no manual browser reload required.
    this.refresh.requests$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadRequests());

    this.loadRequests();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRequests() {
    this.loading  = true;
    this.errorMsg = '';

    this.http.get<any>(`${environment.apiUrl}/requests`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.requests = Array.isArray(res) ? res : (res.data ?? []);
          this.loading  = false;
        },
        error: () => {
          this.errorMsg = 'Could not fetch your requests. Please try again.';
          this.loading  = false;
        }
      });
  }

  // ── Receive entire approved request ──────────────────────────────────────

  receiveAll(requestId: number): void {
    if (this.receivingMap[requestId]) return;
    this.receivingMap[requestId] = true;
    this.successMsg = '';
    this.errorMsg   = '';

    this.workflow.receiveItems(requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.orderSummaryId) {
            this.orderSummaryMap[requestId] = res.orderSummaryId;
          }
          delete this.receivingMap[requestId];
          this.successMsg = `Request #${requestId} received! Order receipt generated.`;

          // ✅ Refresh the request list immediately with latest data from API
          // instead of only mutating the local array (which can get out of sync).
          this.loadRequests();

          // ✅ Also notify OrderHistoryComponent to reload its list.
          this.refresh.notifyOrders();

          setTimeout(() => { this.successMsg = ''; }, 6000);
        },
        error: (err: any) => {
          this.errorMsg = err?.message || 'Failed to confirm receipt.';
          delete this.receivingMap[requestId];
        }
      });
  }

  viewReceipt(requestId: number): void {
    const summaryId = this.orderSummaryMap[requestId];
    if (summaryId) {
      this.router.navigate(['/user-dashboard/order-summary', summaryId]);
    } else {
      this.workflow.getOrderSummaryByRequestId(requestId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (os) => this.router.navigate(['/user-dashboard/order-summary', os.id]),
          error: ()  => this.errorMsg = 'Order receipt not found for this request.'
        });
    }
  }

  // ── Reorder logic ─────────────────────────────────────────────────────────

  openReorderModal(requestId: number): void {
    this.isReorderModalOpen = true;
    this.reorderLoading = true;
    this.reorderSuggestions = [];

    this.http.get<any[]>(`${environment.apiUrl}/requests/${requestId}/reorderable-items`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.reorderSuggestions = res || [];
          this.reorderLoading = false;
        },
        error: (err) => {
          console.error('[ReorderModal] Error fetching reorderable items', err);
          this.errorMsg = err?.error?.message || 'Failed to fetch reorderable items.';
          this.isReorderModalOpen = false;
          this.reorderLoading = false;
        }
      });
  }

  handleReorder(items: any[]): void {
    this.isReorderModalOpen = false;
    items.forEach(item => {
      const mockItem = {
        id: item.itemId,
        name: item.itemName,
        stockLimit: 999,
        availableQuantity: 999,
        categoryId: 0,
        reorderLevel: 0
      } as any;
      this.cart.addItem(mockItem, item.suggestedQuantity);
    });

    this.successMsg = 'Reorder items added to cart! Redirecting...';
    setTimeout(() => {
      this.successMsg = '';
      this.router.navigate(['/user-dashboard/cart']);
    }, 1500);
  }

  // ── Edit Request ──────────────────────────────────────────────────────────

  /** A request is editable only if it is PendingWithIssuer and all items are still PendingWithIssuer */
  isEditable(req: any): boolean {
    if (this.normalizeStatus(req.status) !== 'pendingwithissuer') return false;
    if (!req.items || req.items.length === 0) return false;
    return req.items.every((i: any) => this.normalizeStatus(i.status) === 'pendingwithissuer');
  }

  editRequest(requestId: number): void {
    this.router.navigate(['/user-dashboard/edit-request', requestId]);
  }

  // ── Per-item receive (legacy — kept for backward compat) ──────────────────

  receiveItem(requestId: number, itemId: number) {
    this.http.patch(`${environment.apiUrl}/requests/${requestId}/items/${itemId}/receive`, {})
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          // ✅ Reload from API instead of mutating the local array to guarantee
          //    the displayed data matches the database.
          this.loadRequests();
          this.successMsg = 'Item marked as received.';
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message || 'Failed to mark item as received.';
        }
      });
  }

  // ── Status helpers ────────────────────────────────────────────────────────

  isRequestApproved(req: any): boolean {
    return this.normalizeStatus(req.status) === 'approved';
  }

  isRequestReceived(req: any): boolean {
    return this.normalizeStatus(req.status) === 'received';
  }

  isItemApproved(status: string):  boolean { return this.normalizeStatus(status) === 'approved'; }
  isItemReceived(status: string):  boolean { return this.normalizeStatus(status) === 'received'; }

  hasRejectedItems(req: any): boolean {
    return req.items?.some((i: any) => i.issuerRejectedQuantity > 0) ?? false;
  }

  getStatusIcon(status: string): string {
    const s = this.normalizeStatus(status);
    if (s === 'pendingwithissuer')    return '1';
    if (s === 'notissued')            return '!';
    if (s === 'pendingadminapproval') return '2';
    if (s === 'approved')             return '3';
    if (s === 'rejected')             return 'x';
    if (s === 'received')             return '✓';
    return '-';
  }

  getItemStatusClass  = (s: string) => this.getStatusClass(s);
  getItemStatusLabel  = (s: string) => this.getStatusLabel(s);

  // ── Counters ──────────────────────────────────────────────────────────────

  get pendingCount()  { return this.requests.filter(r => { const s = this.normalizeStatus(r.status); return s === 'pendingwithissuer' || s === 'pendingadminapproval'; }).length; }
  get requestedCount(){ return this.requests.filter(r => this.normalizeStatus(r.status) === 'pendingwithissuer').length; }
  get issuedCount()   { return this.requests.filter(r => this.normalizeStatus(r.status) === 'pendingadminapproval').length; }
  get approvedCount() { return this.requests.filter(r => this.normalizeStatus(r.status) === 'approved').length; }
  get receivedCount() { return this.requests.filter(r => this.normalizeStatus(r.status) === 'received').length; }
  get rejectedCount() {
    return this.requests.filter(r => {
      const s = this.normalizeStatus(r.status);
      return s === 'rejected' || s === 'notissued';
    }).length;
  }
}
