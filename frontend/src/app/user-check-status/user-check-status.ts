import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, computed, signal } from '@angular/core';
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
  styleUrls: ['./user-check-status.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserCheckStatusComponent implements OnInit, OnDestroy {
  normalizeStatus = normalizeStatus;
  getStatusClass = getStatusClass;
  getStatusLabel = getStatusLabel;
  
  // Use signals for reactive state
  requests = signal<any[]>([]);
  loading = signal(true);
  errorMsg = signal('');
  successMsg = signal('');
  
  // Computed counters for performance
  pendingCount = computed(() => {
    return this.requests().filter(r => {
      const s = this.normalizeStatus(r.status);
      return s === 'pendingwithissuer' || s === 'pendingadminapproval';
    }).length;
  });
  
  requestedCount = computed(() => 
    this.requests().filter(r => this.normalizeStatus(r.status) === 'pendingwithissuer').length
  );
  
  issuedCount = computed(() => 
    this.requests().filter(r => this.normalizeStatus(r.status) === 'pendingadminapproval').length
  );
  
  approvedCount = computed(() => 
    this.requests().filter(r => this.normalizeStatus(r.status) === 'approved').length
  );
  
  receivedCount = computed(() => 
    this.requests().filter(r => this.normalizeStatus(r.status) === 'received').length
  );
  
  rejectedCount = computed(() => {
    return this.requests().filter(r => {
      const s = this.normalizeStatus(r.status);
      return s === 'rejected' || s === 'notissued';
    }).length;
  });

  /** requestId → true while "Receive All" call is in flight */
  receivingMap: { [requestId: number]: boolean } = {};

  /** requestId → orderSummaryId once received */
  orderSummaryMap: { [requestId: number]: number } = {};

  // Reorder Modal State
  isReorderModalOpen = false;
  reorderSuggestions: any[] = [];
  reorderLoading = false;

  // Receive Confirmation Dialog State
  isReceiveConfirmDialogOpen = false;
  receiveConfirmRequestId: number | null = null;

  // Receipt Modal State
  isReceiptModalOpen = false;
  currentReceipt: any = null;
  receiptLoading = false;
  receiptError = signal('');
  receivingFromModal = false;
  generatedDate = new Date();
  
  // Memoized receipt totals
  private receiptTotals = computed(() => {
    if (!this.currentReceipt?.items) return {
      requested: 0,
      issued: 0,
      rejected: 0,
      approved: 0,
      received: 0
    };
    
    const items = this.currentReceipt.items;
    return {
      requested: items.reduce((sum: number, item: any) => sum + (item.quantityRequested || 0), 0),
      issued: items.reduce((sum: number, item: any) => sum + (item.issuerIssuedQuantity || 0), 0),
      rejected: items.reduce((sum: number, item: any) => sum + (item.issuerRejectedQuantity || 0) + (item.adminRejectedQuantity || 0), 0),
      approved: items.reduce((sum: number, item: any) => sum + (item.adminApprovedQuantity || 0), 0),
      received: items.reduce((sum: number, item: any) => {
        if (this.isItemReceived(item.status)) {
          return sum + (item.adminApprovedQuantity || item.quantityRequested || 0);
        }
        return sum;
      }, 0)
    };
  });

  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private workflow: WorkflowService,
    private cart: CartService,
    private router: Router,
    private refresh: RefreshService,
    private cdr: ChangeDetectorRef
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
    this.loading.set(true);
    this.errorMsg.set('');

    this.http.get<any>(`${environment.apiUrl}/requests`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          if (environment.production) {
            console.log('[UserCheckStatus] loadRequests API response:', res);
          }
          const rawRequests = Array.isArray(res) ? res : (res.data ?? []);
          
          // Normalize request objects to ensure they have 'id' property
          const normalizedRequests = rawRequests.map((req: any) => ({
            ...req,
            id: req.id || req.Id || req.requestId || req.RequestId
          }));
          
          this.requests.set(normalizedRequests);
          this.loading.set(false);
          this.cdr.markForCheck();
        },
        error: (err) => {
          console.error('[UserCheckStatus] loadRequests error:', err);
          this.errorMsg.set('Could not fetch your requests. Please try again.');
          this.loading.set(false);
          this.cdr.markForCheck();
        }
      });
  }

  // ── Receive entire approved request ──────────────────────────────────────

  openReceiveConfirmDialog(requestId: number): void {
    if (environment.production) {
      console.log('[UserCheckStatus] openReceiveConfirmDialog called with requestId:', requestId);
    }
    this.receiveConfirmRequestId = requestId;
    this.isReceiveConfirmDialogOpen = true;
    this.cdr.markForCheck();
  }

  closeReceiveConfirmDialog(): void {
    if (environment.production) {
      console.log('[UserCheckStatus] closeReceiveConfirmDialog called');
    }
    this.isReceiveConfirmDialogOpen = false;
    this.receiveConfirmRequestId = null;
    this.cdr.markForCheck();
  }

  confirmReceive(): void {
    if (environment.production) {
      console.log('[UserCheckStatus] confirmReceive called');
    }
    
    if (!this.receiveConfirmRequestId) {
      console.error('[UserCheckStatus] receiveConfirmRequestId is null/undefined in confirmReceive');
      return;
    }
    
    const requestIdToReceive = this.receiveConfirmRequestId;
    this.closeReceiveConfirmDialog();
    this.receiveAll(requestIdToReceive);
  }

  receiveAll(requestId: number): void {
    if (environment.production) {
      console.log('[UserCheckStatus] receiveAll called with requestId:', requestId);
    }
    
    if (!requestId || requestId <= 0) {
      console.error('[UserCheckStatus] Invalid request ID:', requestId);
      this.errorMsg.set('Invalid request ID. Cannot confirm receipt.');
      this.cdr.markForCheck();
      return;
    }

    if (this.receivingMap[requestId]) return;
    this.receivingMap[requestId] = true;
    this.successMsg.set('');
    this.errorMsg.set('');
    this.cdr.markForCheck();
    
    this.workflow.receiveItems(requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (environment.production) {
            console.log('[UserCheckStatus] Receive API response:', res);
          }
          if (res.orderSummaryId) {
            this.orderSummaryMap[requestId] = res.orderSummaryId;
          }
          delete this.receivingMap[requestId];
          this.successMsg.set(`Request #${requestId} received! Order receipt generated.`);

          // Refresh the request list immediately with latest data from API
          this.loadRequests();

          // Also notify OrderHistoryComponent to reload its list.
          this.refresh.notifyOrders();

          setTimeout(() => { 
            this.successMsg.set('');
            this.cdr.markForCheck();
          }, 6000);
        },
        error: (err: any) => {
          console.error('[UserCheckStatus] Receive API error:', err);
          this.errorMsg.set(err?.message || 'Failed to confirm receipt.');
          delete this.receivingMap[requestId];
          this.cdr.markForCheck();
        }
      });
  }

  viewReceipt(requestId: number): void {
    // Validate request ID before making API call
    if (!requestId || requestId <= 0) {
      this.errorMsg.set('Invalid request ID. Cannot view receipt.');
      this.cdr.markForCheck();
      return;
    }

    const summaryId = this.orderSummaryMap[requestId];
    if (summaryId) {
      this.router.navigate(['/user-dashboard/order-summary', summaryId]);
    } else {
      this.workflow.getOrderSummaryByRequestId(requestId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (os) => {
            if (os && os.id) {
              this.orderSummaryMap[requestId] = os.id;
              this.router.navigate(['/user-dashboard/order-summary', os.id]);
            } else {
              this.errorMsg.set('Order receipt not found for this request.');
              this.cdr.markForCheck();
            }
          },
          error: ()  => {
            this.errorMsg.set('Order receipt not found for this request.');
            this.cdr.markForCheck();
          }
        });
    }
  }

  // ── Receipt Modal ─────────────────────────────────────────────────────────────

  openReceiptModal(requestId: number): void {
    if (environment.production) {
      console.log('[UserCheckStatus] openReceiptModal called with requestId:', requestId);
    }
    
    // Validate request ID before opening modal
    if (!requestId || requestId <= 0) {
      console.error('[UserCheckStatus] Invalid request ID:', requestId);
      this.errorMsg.set('Invalid request ID. Cannot view receipt.');
      this.cdr.markForCheck();
      return;
    }

    this.isReceiptModalOpen = true;
    this.receiptLoading = true;
    this.receiptError.set('');
    this.currentReceipt = null;
    this.generatedDate = new Date();
    this.cdr.markForCheck();

    // Find the request in our local data first
    const request = this.requests().find(r => r.id === requestId);
    
    if (request) {
      this.currentReceipt = request;
      this.receiptLoading = false;
      this.cdr.markForCheck();
    } else {
      // If not found locally, try to fetch from API
      this.http.get<any>(`${environment.apiUrl}/requests/${requestId}`)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (data) => {
            // Normalize to ensure 'id' property exists
            const normalized = {
              ...data,
              id: data.id || data.Id || data.requestId || data.RequestId || requestId
            };
            this.currentReceipt = normalized;
            this.receiptLoading = false;
            this.cdr.markForCheck();
          },
          error: (err) => {
            console.error('[UserCheckStatus] API error fetching request:', err);
            this.receiptError.set('Failed to load receipt details. Please try again.');
            this.receiptLoading = false;
            this.cdr.markForCheck();
          }
        });
    }
  }

  closeReceiptModal(): void {
    this.isReceiptModalOpen = false;
    this.currentReceipt = null;
    this.receiptError.set('');
    this.receivingFromModal = false;
    this.cdr.markForCheck();
  }

  canShowReceivedButton(): boolean {
    return this.currentReceipt && this.isRequestApproved(this.currentReceipt);
  }

  confirmReceiptFromModal(): void {
    if (!this.currentReceipt || this.receivingFromModal) return;

    this.receivingFromModal = true;
    const requestId = this.currentReceipt.id;
    this.cdr.markForCheck();

    // Validate request ID before making API call
    if (!requestId || requestId <= 0) {
      console.error('[UserCheckStatus] Invalid request ID from receipt:', requestId);
      this.receiptError.set('Invalid request ID. Cannot confirm receipt.');
      this.receivingFromModal = false;
      this.cdr.markForCheck();
      return;
    }

    this.workflow.receiveItems(requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (environment.production) {
            console.log('[UserCheckStatus] Receive API response:', res);
          }
          if (res.orderSummaryId) {
            this.orderSummaryMap[requestId] = res.orderSummaryId;
          }
          this.receivingFromModal = false;
          this.successMsg.set(`Request #${requestId} received! Order receipt generated.`);

          // Refresh the request list
          this.loadRequests();

          // Notify other components
          this.refresh.notifyOrders();

          // Close modal after short delay
          setTimeout(() => {
            this.closeReceiptModal();
            this.successMsg.set('');
            this.cdr.markForCheck();
          }, 2000);
        },
        error: (err: any) => {
          console.error('[UserCheckStatus] Receive API error:', err);
          this.receiptError.set(err?.message || 'Failed to confirm receipt. Please try again.');
          this.receivingFromModal = false;
          this.cdr.markForCheck();
        }
      });
  }

  // ── Receipt Helper Methods ─────────────────────────────────────────────────────

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  formatDateTime(date: Date | string): string {
    return new Date(date).toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  // Use computed totals for performance
  getTotalRequested(): number { return this.receiptTotals().requested; }
  getTotalIssued(): number { return this.receiptTotals().issued; }
  getTotalRejected(): number { return this.receiptTotals().rejected; }
  getTotalApproved(): number { return this.receiptTotals().approved; }
  getTotalReceived(): number { return this.receiptTotals().received; }

  // ── Reorder logic ─────────────────────────────────────────────────────────

  openReorderModal(requestId: number): void {
    // Validate request ID before opening modal
    if (!requestId || requestId <= 0) {
      this.errorMsg.set('Invalid request ID. Cannot view reorder options.');
      this.cdr.markForCheck();
      return;
    }

    this.isReorderModalOpen = true;
    this.reorderLoading = true;
    this.reorderSuggestions = [];
    this.cdr.markForCheck();

    this.http.get<any[]>(`${environment.apiUrl}/requests/${requestId}/reorderable-items`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.reorderSuggestions = res || [];
          this.reorderLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          console.error('[ReorderModal] Error fetching reorderable items', err);
          this.errorMsg.set(err?.error?.message || 'Failed to fetch reorderable items.');
          this.isReorderModalOpen = false;
          this.reorderLoading = false;
          this.cdr.markForCheck();
        }
      });
  }

  handleReorder(items: any[]): void {
    this.isReorderModalOpen = false;
    items.forEach(item => {
      const mockItem = {
        id: item.itemCode,
        name: item.itemName,
        stockLimit: 999,
        availableQuantity: 999,
        categoryId: 0,
        reorderLevel: 0
      } as any;
      this.cart.addItem(mockItem, item.suggestedQuantity);
    });

    this.successMsg.set('Reorder items added to cart! Redirecting...');
    this.cdr.markForCheck();
    setTimeout(() => {
      this.successMsg.set('');
      this.router.navigate(['/user-dashboard/cart']);
    }, 1500);
  }

  // ── Edit Request ──────────────────────────────────────────────────────────

  /**
   * A request is editable only when:
   *  - request status is PendingWithIssuer (or legacy alias "Requested")
   *  - it has at least one item
   *  - every item is still PendingWithIssuer (issuer has not touched any item)
   *
   * The list endpoint (GET /api/requests) returns status as a plain string via
   * .ToString(), e.g. "PendingWithIssuer".  normalizeStatus() lowercases it so
   * the comparison is case-insensitive.
   */
  isEditable(req: any): boolean {
    if (!req) return false;
    const reqStatus = this.normalizeStatus(req.status);
    if (reqStatus !== 'pendingwithissuer' && reqStatus !== 'requested') return false;
    const items: any[] = req.items ?? [];
    if (items.length === 0) return false;
    return items.every(
      (i: any) => {
        const s = this.normalizeStatus(i.status);
        return s === 'pendingwithissuer' || s === 'requested';
      }
    );
  }

  /** Navigate to the edit page for the given request. */
  editRequest(requestId: number): void {
    // Validate request ID before navigation
    if (!requestId || requestId <= 0) {
      this.errorMsg.set('Invalid request ID. Cannot edit request.');
      this.cdr.markForCheck();
      return;
    }
    this.router.navigate(['/user-dashboard/edit-request', requestId]);
  }

  // ── Per-item receive (legacy — kept for backward compat) ──────────────────

  receiveItem(requestId: number, itemCode: string) {
    this.http.patch(`${environment.apiUrl}/requests/${requestId}/items/${itemCode}/receive`, {})
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          // Reload from API instead of mutating the local array
          this.loadRequests();
          this.successMsg.set('Item marked as received.');
          this.cdr.markForCheck();
          setTimeout(() => { 
            this.successMsg.set('');
            this.cdr.markForCheck();
          }, 3000);
        },
        error: (err: any) => {
          this.errorMsg.set(err?.error?.message || 'Failed to mark item as received.');
          this.cdr.markForCheck();
        }
      });
  }

  // ── Status helpers ────────────────────────────────────────────────────────

  isRequestApproved(req: any): boolean {
    // ✅ Show Receive button whenever the request is Approved (ReadyToReceive).
    // This covers partial-issue scenarios where some items are NotIssued and
    // the remaining approved items are ready for the user to collect.
    return this.normalizeStatus(req.status) === 'approved';
  }

  isRequestReceived(req: any): boolean {
    return this.normalizeStatus(req.status) === 'received';
  }

  isItemApproved(status: string):  boolean { return this.normalizeStatus(status) === 'approved'; }
  isItemReceived(status: string):  boolean { 
    const s = this.normalizeStatus(status);
    return s === 'received' || s === 'approved'; // Show received for approved items too in modal
  }

  hasRejectedItems(req: any): boolean {
    // ✅ Show the reorder prompt when any item was rejected at either stage.
    return req.items?.some(
      (i: any) => (i.issuerRejectedQuantity ?? 0) > 0 || (i.adminRejectedQuantity ?? 0) > 0
    ) ?? false;
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

  // ── TrackBy Functions for ngFor ───────────────────────────────────────────────
  
  trackByRequestId(index: number, req: any): number {
    return req.id;
  }
  
  trackByItemCode(index: number, item: any): string | number {
    return item.id || item.itemCode || index;
  }
  
  // ── Counters (now using computed signals) ───────────────────────────────────────
  // Counters are now computed signals defined in constructor
}
