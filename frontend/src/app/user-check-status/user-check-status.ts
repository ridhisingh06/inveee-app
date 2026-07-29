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

// TypeScript Interfaces
interface RequestItem {
  id: number;
  itemCode: string;
  itemName: string;
  quantityRequested: number;
  quantityApproved: number;
  quantityIssued: number;
  issuerIssuedQuantity: number;
  issuerRejectedQuantity: number;
  adminApprovedQuantity: number;
  adminRejectedQuantity: number;
  receivedQuantity: number;
  status: string;
  normalizedStatus?: string;
}

interface Request {
  id: number;
  userId: number;
  status: string;
  createdAt: string;
  updatedAt: string | null;
  items: RequestItem[];
  issuerName?: string;
  adminName?: string;
  normalizedStatus?: string;
}

interface ReceiptTotals {
  requested: number;
  issued: number;
  rejected: number;
  approved: number;
  received: number;
}

interface ReorderableItem {
  itemCode: string;
  itemName: string;
  suggestedQuantity: number;
}

// Constants
const SUCCESS_MESSAGE_TIMEOUT = 6000;
const MODAL_CLOSE_TIMEOUT = 2000;
const REDIRECT_TIMEOUT = 1500;
const REQUEST_STATUS = {
  PENDING_WITH_ISSUER: 'pendingwithissuer',
  REQUESTED: 'requested',
  PENDING_ADMIN_APPROVAL: 'pendingadminapproval',
  APPROVED: 'approved',
  RECEIVED: 'received',
  REJECTED: 'rejected',
  NOT_ISSUED: 'notissued'
} as const;

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
  requests = signal<Request[]>([]);
  loading = signal(true);
  errorMsg = signal('');
  successMsg = signal('');
  
  // Computed counters for performance
  pendingCount = computed(() => {
    return this.requests().filter(r => {
      const s = r.normalizedStatus;
      return s === 'pendingwithissuer' || s === 'pendingadminapproval';
    }).length;
  });
  
  requestedCount = computed(() => 
    this.requests().filter(r => r.normalizedStatus === 'pendingwithissuer').length
  );
  
  issuedCount = computed(() => 
    this.requests().filter(r => r.normalizedStatus === 'pendingadminapproval').length
  );
  
  approvedCount = computed(() => 
    this.requests().filter(r => r.normalizedStatus === 'approved').length
  );
  
  receivedCount = computed(() => 
    this.requests().filter(r => r.normalizedStatus === 'received').length
  );
  
  rejectedCount = computed(() => {
    return this.requests().filter(r => {
      const s = r.normalizedStatus;
      return s === 'rejected' || s === 'notissued';
    }).length;
  });

  /** requestId → true while "Receive All" call is in flight */
  receivingMap: { [requestId: number]: boolean } = {};

  /** requestId → orderSummaryId once received */
  orderSummaryMap: { [requestId: number]: number } = {};

  // Reorder Modal State
  isReorderModalOpen = false;
  reorderSuggestions: ReorderableItem[] = [];
  reorderLoading = false;

  // Receive Confirmation Dialog State
  isReceiveConfirmDialogOpen = signal(false);
  receiveConfirmRequestId = signal<number | null>(null);

  // Receipt Modal State
  isReceiptModalOpen = signal(false);
  currentReceipt = signal<Request | null>(null);
  receiptLoading = signal(false);
  receiptError = signal('');
  receivingFromModal = signal(false);
  generatedDate = new Date();
  
  // Memoized receipt totals
  private receiptTotals = computed((): ReceiptTotals => {
    if (!this.currentReceipt()?.items) return {
      requested: 0,
      issued: 0,
      rejected: 0,
      approved: 0,
      received: 0
    };
    
    const items = this.currentReceipt()!.items;
    return {
      requested: items.reduce((sum: number, item: RequestItem) => sum + (item.quantityRequested || 0), 0),
      issued: items.reduce((sum: number, item: RequestItem) => sum + (item.issuerIssuedQuantity || 0), 0),
      rejected: items.reduce((sum: number, item: RequestItem) => sum + (item.issuerRejectedQuantity || 0) + (item.adminRejectedQuantity || 0), 0),
      approved: items.reduce((sum: number, item: RequestItem) => sum + (item.adminApprovedQuantity || 0), 0),
      received: items.reduce((sum: number, item: RequestItem) => {
        // Helper to determine if item is received - logic should be consistent with app
        return sum + (item.receivedQuantity || 0);
      }, 0)
    };
  });

  private destroy$ = new Subject<void>();
  private timeoutIds: number[] = [];

  constructor(
    private http: HttpClient,
    private workflow: WorkflowService,
    private cart: CartService,
    private router: Router,
    private refresh: RefreshService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.refresh.requests$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadRequests());

    this.loadRequests();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    
    this.timeoutIds.forEach(id => clearTimeout(id));
    this.timeoutIds = [];
  }

  loadRequests() {
    this.loading.set(true);
    this.errorMsg.set('');

    this.http.get<any>(`${environment.apiUrl}/requests`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          const rawRequests = Array.isArray(res) ? res : (res.data ?? []);
          
          const normalizedRequests = rawRequests.map((req: any) => ({
            ...req,
            id: req.id || req.Id || req.requestId || req.RequestId,
            normalizedStatus: this.normalizeStatus(req.status),
            items: req.items?.map((item: any) => ({
              ...item,
              normalizedStatus: this.normalizeStatus(item.status)
            })) || []
          }));
          
          this.requests.set(normalizedRequests);
          this.loading.set(false);
        },
        error: (err) => {
          this.errorMsg.set('Could not fetch your requests. Please try again.');
          this.loading.set(false);
        }
      });
  }

  // ── Private Helper Methods ─────────────────────────────────────────────────────

  private isValidRequestId(requestId: number): boolean {
    return requestId != null && requestId > 0;
  }

  // ── Receive entire approved request ──────────────────────────────────────

  // Open the confirm receipt dialog for a given request ID
  openReceiveConfirmDialog(requestId: number): void {
    if (!this.isValidRequestId(requestId)) { return; }
    this.receiveConfirmRequestId.set(requestId);
    this.isReceiveConfirmDialogOpen.set(true);
    this.cdr.detectChanges();
  }

  // Close the confirm receipt dialog
  closeReceiveConfirmDialog(): void {
    this.isReceiveConfirmDialogOpen.set(false);
    this.receiveConfirmRequestId.set(null);
    this.cdr.detectChanges();
  }

  // Confirm receipt action after dialog confirmation
  confirmReceive(): void {
    const requestIdToReceive = this.receiveConfirmRequestId();
    if (!requestIdToReceive) {
      return;
    }
    this.closeReceiveConfirmDialog();
    this.receiveAll(requestIdToReceive);
  }

  receiveAll(requestId: number): void {
    if (!this.isValidRequestId(requestId)) {
      this.errorMsg.set('Invalid request ID. Cannot confirm receipt.');
      return;
    }

    if (this.receivingMap[requestId]) {
      return;
    }
    
    this.receivingMap[requestId] = true;
    this.successMsg.set('');
    this.errorMsg.set('');
    
    this.workflow.receiveItems(requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.orderSummaryId) {
            this.orderSummaryMap[requestId] = res.orderSummaryId;
          }
          delete this.receivingMap[requestId];
          this.successMsg.set(`Request #${requestId} received! Order receipt generated.`);

          // Refresh the request list immediately with latest data from API
          this.loadRequests();

          // Also notify OrderHistoryComponent to reload its list.
          this.refresh.notifyOrders();

          const timeoutId = setTimeout(() => { 
            this.successMsg.set('');
          }, SUCCESS_MESSAGE_TIMEOUT);
          this.timeoutIds.push(timeoutId);
        },
        error: (err: any) => {
          this.errorMsg.set(err?.message || 'Failed to confirm receipt.');
          delete this.receivingMap[requestId];
        }
      });
  }

  viewReceipt(requestId: number): void {
    if (!this.isValidRequestId(requestId)) {
      this.errorMsg.set('Invalid request ID. Cannot view receipt.');
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
            }
          },
          error: (err)  => {
            this.errorMsg.set('Order receipt not found for this request.');
          }
        });
    }
  }

  // ── Receipt Modal ─────────────────────────────────────────────────────────────

  openReceiptModal(requestId: number): void {
    if (!this.isValidRequestId(requestId)) { return; }
    this.isReceiptModalOpen.set(true);
    this.receiptLoading.set(true);
    this.receiptError.set('');
    this.currentReceipt.set(null);
    this.generatedDate = new Date();

    // Find the request in our local data first
    const request = this.requests().find(r => r.id === requestId);
    
    if (request) {
      this.currentReceipt.set(request);
      this.receiptLoading.set(false);
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
            this.currentReceipt.set(normalized);
            this.receiptLoading.set(false);
          },
          error: (err) => {
            this.receiptError.set('Failed to load receipt details. Please try again.');
            this.receiptLoading.set(false);
          }
        });
    }
  }

  closeReceiptModal(): void {
    this.isReceiptModalOpen.set(false);
    this.currentReceipt.set(null);
    this.receiptError.set('');
    this.receivingFromModal.set(false);
  }

  canShowReceivedButton(): boolean {
    const receipt = this.currentReceipt();
    if (!receipt) {
      return false;
    }
    return this.isRequestApproved(receipt);
  }

  confirmReceiptFromModal(): void {
    const receipt = this.currentReceipt();
    if (!receipt || this.receivingFromModal()) return;

    this.receivingFromModal.set(true);
    const requestId = receipt.id;

    // Validate request ID before making API call
    if (!this.isValidRequestId(requestId)) {
      this.receiptError.set('Invalid request ID. Cannot confirm receipt.');
      this.receivingFromModal.set(false);
      return;
    }

    this.workflow.receiveItems(requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.orderSummaryId) {
            this.orderSummaryMap[requestId] = res.orderSummaryId;
          }
          this.receivingFromModal.set(false);
          this.successMsg.set(`Request #${requestId} received! Order receipt generated.`);

          // Refresh the request list and current receipt
          this.loadRequests();
          this.openReceiptModal(requestId);

          // Notify other components
          this.refresh.notifyOrders();

          // Close modal after short delay
          const timeoutId = setTimeout(() => {
            this.closeReceiptModal();
            this.successMsg.set('');
          }, MODAL_CLOSE_TIMEOUT);
          this.timeoutIds.push(timeoutId);
        },
        error: (err: any) => {
          this.receiptError.set(err?.message || 'Failed to confirm receipt. Please try again.');
          this.receivingFromModal.set(false);
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
    if (!this.isValidRequestId(requestId)) {
      this.errorMsg.set('Invalid request ID. Cannot view reorder options.');
      return;
    }

    this.isReorderModalOpen = true;
    this.reorderLoading = true;
    this.reorderSuggestions = [];

    this.http.get<ReorderableItem[]>(`${environment.apiUrl}/requests/${requestId}/reorderable-items`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.reorderSuggestions = res || [];
          this.reorderLoading = false;
        },
        error: (err) => {
          this.errorMsg.set(err?.error?.message || 'Failed to fetch reorderable items.');
          this.isReorderModalOpen = false;
          this.reorderLoading = false;
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
    const timeoutId = setTimeout(() => {
      this.successMsg.set('');
      this.router.navigate(['/user-dashboard/cart']);
    }, REDIRECT_TIMEOUT);
    this.timeoutIds.push(timeoutId);
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
  isEditable(req: Request): boolean {
    if (!req) return false;
    const reqStatus = req.normalizedStatus;
    if (reqStatus !== REQUEST_STATUS.PENDING_WITH_ISSUER && reqStatus !== REQUEST_STATUS.REQUESTED) return false;
    const items: RequestItem[] = req.items ?? [];
    if (items.length === 0) return false;
    return items.every(
      (i: RequestItem) => {
        const s = (i as any).normalizedStatus;
        return s === REQUEST_STATUS.PENDING_WITH_ISSUER || s === REQUEST_STATUS.REQUESTED;
      }
    );
  }

  /** Navigate to the edit page for the given request. */
  editRequest(requestId: number): void {
    if (!this.isValidRequestId(requestId)) {
      this.errorMsg.set('Invalid request ID. Cannot edit request.');
      return;
    }
    this.router.navigate(['/user-dashboard/edit-request', requestId]);
  }

  // ── Per-item receive (legacy — kept for backward compat) ──────────────────


  // ── Status helpers ────────────────────────────────────────────────────────

  isRequestApproved(req: Request): boolean {
    // ✅ Show Receive button whenever the request is Approved (ReadyToReceive).
    // This covers partial-issue scenarios where some items are NotIssued and
    // the remaining approved items are ready for the user to collect.
    return req.normalizedStatus === REQUEST_STATUS.APPROVED;
  }

  isRequestReceived(req: Request): boolean {
    return req.normalizedStatus === REQUEST_STATUS.RECEIVED;
  }

  isItemApproved(status: string):  boolean { return this.normalizeStatus(status) === 'approved'; }
  isItemReceived(status: string): boolean {
    const s = this.normalizeStatus(status);
    return s === 'received'; // Only true when truly received
  }

  hasRejectedItems(req: Request): boolean {
    // ✅ Show the reorder prompt when any item was rejected at either stage.
    return req.items?.some(
      (i: RequestItem) => (i.issuerRejectedQuantity ?? 0) > 0 || (i.adminRejectedQuantity ?? 0) > 0
    ) ?? false;
  }

  getStatusIcon(status: string): string {
    const s = this.normalizeStatus(status);
    if (s === REQUEST_STATUS.PENDING_WITH_ISSUER) return '1';
    if (s === REQUEST_STATUS.NOT_ISSUED) return '!';
    if (s === REQUEST_STATUS.PENDING_ADMIN_APPROVAL) return '2';
    if (s === REQUEST_STATUS.APPROVED) return '3';
    if (s === REQUEST_STATUS.REJECTED) return 'x';
    if (s === REQUEST_STATUS.RECEIVED) return '✓';
    return '-';
  }

  getItemStatusClass  = (s: string) => this.getStatusClass(s);
  getItemStatusLabel  = (s: string) => this.getStatusLabel(s);

  // ── TrackBy Functions for ngFor ───────────────────────────────────────────────
  
  trackByRequestId(index: number, req: Request): number {
    return req.id;
  }
  
  trackByItemCode(index: number, item: RequestItem): string | number {
    return item.id || item.itemCode || index;
  }
  
  // ── Counters (now using computed signals) ───────────────────────────────────────
  // Counters are now computed signals defined in constructor
}
