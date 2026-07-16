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

  // Receive Confirmation Dialog State
  isReceiveConfirmDialogOpen = false;
  receiveConfirmRequestId: number | null = null;

  // Receipt Modal State
  isReceiptModalOpen = false;
  currentReceipt: any = null;
  receiptLoading = false;
  receiptError = '';
  receivingFromModal = false;
  generatedDate = new Date();

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
          console.log('[UserCheckStatus] loadRequests API response:', res);
          const rawRequests = Array.isArray(res) ? res : (res.data ?? []);
          
          // Normalize request objects to ensure they have 'id' property
          this.requests = rawRequests.map((req: any) => {
            // Handle both 'id' and 'Id' (uppercase) from backend
            const normalized = {
              ...req,
              id: req.id || req.Id || req.requestId || req.RequestId
            };
            console.log('[UserCheckStatus] Normalized request:', normalized);
            return normalized;
          });
          
          console.log('[UserCheckStatus] Final requests array:', this.requests);
          this.loading  = false;
        },
        error: (err) => {
          console.error('[UserCheckStatus] loadRequests error:', err);
          this.errorMsg = 'Could not fetch your requests. Please try again.';
          this.loading  = false;
        }
      });
  }

  // ── Receive entire approved request ──────────────────────────────────────

  openReceiveConfirmDialog(requestId: number): void {
    console.log('[UserCheckStatus] openReceiveConfirmDialog called with requestId:', requestId);
    this.receiveConfirmRequestId = requestId;
    console.log('[UserCheckStatus] receiveConfirmRequestId set to:', this.receiveConfirmRequestId);
    this.isReceiveConfirmDialogOpen = true;
  }

  closeReceiveConfirmDialog(): void {
    console.log('[UserCheckStatus] closeReceiveConfirmDialog called');
    this.isReceiveConfirmDialogOpen = false;
    this.receiveConfirmRequestId = null;
    console.log('[UserCheckStatus] receiveConfirmRequestId reset to null');
  }

  confirmReceive(): void {
    console.log('[UserCheckStatus] confirmReceive called');
    console.log('[UserCheckStatus] receiveConfirmRequestId before check:', this.receiveConfirmRequestId);
    
    if (!this.receiveConfirmRequestId) {
      console.error('[UserCheckStatus] receiveConfirmRequestId is null/undefined in confirmReceive');
      return;
    }
    
    console.log('[UserCheckStatus] Calling receiveAll with receiveConfirmRequestId:', this.receiveConfirmRequestId);
    this.closeReceiveConfirmDialog();
    this.receiveAll(this.receiveConfirmRequestId);
  }

  receiveAll(requestId: number): void {
    console.log('[UserCheckStatus] receiveAll called with requestId:', requestId);
    
    if (!requestId || requestId <= 0) {
      console.error('[UserCheckStatus] Invalid request ID:', requestId);
      this.errorMsg = 'Invalid request ID. Cannot confirm receipt.';
      return;
    }

    if (this.receivingMap[requestId]) return;
    this.receivingMap[requestId] = true;
    this.successMsg = '';
    this.errorMsg   = '';

    console.log('[UserCheckStatus] Calling receive API with requestId:', requestId);
    
    this.workflow.receiveItems(requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          console.log('[UserCheckStatus] Receive API response:', res);
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
          console.error('[UserCheckStatus] Receive API error:', err);
          this.errorMsg = err?.message || 'Failed to confirm receipt.';
          delete this.receivingMap[requestId];
        }
      });
  }

  viewReceipt(requestId: number): void {
    // Validate request ID before making API call
    if (!requestId || requestId <= 0) {
      this.errorMsg = 'Invalid request ID. Cannot view receipt.';
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
              this.errorMsg = 'Order receipt not found for this request.';
            }
          },
          error: ()  => this.errorMsg = 'Order receipt not found for this request.'
        });
    }
  }

  // ── Receipt Modal ─────────────────────────────────────────────────────────────

  openReceiptModal(requestId: number): void {
    console.log('[UserCheckStatus] openReceiptModal called with requestId:', requestId);
    
    // Validate request ID before opening modal
    if (!requestId || requestId <= 0) {
      console.error('[UserCheckStatus] Invalid request ID:', requestId);
      this.errorMsg = 'Invalid request ID. Cannot view receipt.';
      return;
    }

    this.isReceiptModalOpen = true;
    this.receiptLoading = true;
    this.receiptError = '';
    this.currentReceipt = null;
    this.generatedDate = new Date();

    // Find the request in our local data first
    const request = this.requests.find(r => r.id === requestId);
    console.log('[UserCheckStatus] Found request in local array:', request);
    
    if (request) {
      this.currentReceipt = request;
      console.log('[UserCheckStatus] currentReceipt set from local array:', this.currentReceipt);
      this.receiptLoading = false;
    } else {
      console.log('[UserCheckStatus] Request not found locally, fetching from API');
      // If not found locally, try to fetch from API
      this.http.get<any>(`${environment.apiUrl}/requests/${requestId}`)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (data) => {
            console.log('[UserCheckStatus] API response data:', data);
            // Normalize to ensure 'id' property exists
            const normalized = {
              ...data,
              id: data.id || data.Id || data.requestId || data.RequestId || requestId
            };
            this.currentReceipt = normalized;
            console.log('[UserCheckStatus] currentReceipt set from API (normalized):', this.currentReceipt);
            this.receiptLoading = false;
          },
          error: (err) => {
            console.error('[UserCheckStatus] API error fetching request:', err);
            this.receiptError = 'Failed to load receipt details. Please try again.';
            this.receiptLoading = false;
          }
        });
    }
  }

  closeReceiptModal(): void {
    this.isReceiptModalOpen = false;
    this.currentReceipt = null;
    this.receiptError = '';
    this.receivingFromModal = false;
  }

  canShowReceivedButton(): boolean {
    return this.currentReceipt && this.isRequestApproved(this.currentReceipt);
  }

  confirmReceiptFromModal(): void {
    if (!this.currentReceipt || this.receivingFromModal) return;

    this.receivingFromModal = true;
    const requestId = this.currentReceipt.id;

    console.log('[UserCheckStatus] confirmReceiptFromModal called with requestId:', requestId);
    console.log('[UserCheckStatus] currentReceipt:', this.currentReceipt);

    // Validate request ID before making API call
    if (!requestId || requestId <= 0) {
      console.error('[UserCheckStatus] Invalid request ID from receipt:', requestId);
      this.receiptError = 'Invalid request ID. Cannot confirm receipt.';
      this.receivingFromModal = false;
      return;
    }

    console.log('[UserCheckStatus] Calling receive API with requestId:', requestId);

    this.workflow.receiveItems(requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          console.log('[UserCheckStatus] Receive API response:', res);
          if (res.orderSummaryId) {
            this.orderSummaryMap[requestId] = res.orderSummaryId;
          }
          this.receivingFromModal = false;
          this.successMsg = `Request #${requestId} received! Order receipt generated.`;

          // Refresh the request list
          this.loadRequests();

          // Notify other components
          this.refresh.notifyOrders();

          // Close modal after short delay
          setTimeout(() => {
            this.closeReceiptModal();
            this.successMsg = '';
          }, 2000);
        },
        error: (err: any) => {
          console.error('[UserCheckStatus] Receive API error:', err);
          this.receiptError = err?.message || 'Failed to confirm receipt. Please try again.';
          this.receivingFromModal = false;
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

  getTotalRequested(): number {
    if (!this.currentReceipt?.items) return 0;
    return this.currentReceipt.items.reduce((sum: number, item: any) => 
      sum + (item.quantityRequested || 0), 0);
  }

  getTotalIssued(): number {
    if (!this.currentReceipt?.items) return 0;
    return this.currentReceipt.items.reduce((sum: number, item: any) => 
      sum + (item.issuerIssuedQuantity || 0), 0);
  }

  getTotalRejected(): number {
    if (!this.currentReceipt?.items) return 0;
    return this.currentReceipt.items.reduce((sum: number, item: any) => 
      sum + (item.issuerRejectedQuantity || 0) + (item.adminRejectedQuantity || 0), 0);
  }

  getTotalApproved(): number {
    if (!this.currentReceipt?.items) return 0;
    return this.currentReceipt.items.reduce((sum: number, item: any) => 
      sum + (item.adminApprovedQuantity || 0), 0);
  }

  getTotalReceived(): number {
    if (!this.currentReceipt?.items) return 0;
    return this.currentReceipt.items.reduce((sum: number, item: any) => {
      if (this.isItemReceived(item.status)) {
        return sum + (item.adminApprovedQuantity || item.quantityRequested || 0);
      }
      return sum;
    }, 0);
  }

  // ── Reorder logic ─────────────────────────────────────────────────────────

  openReorderModal(requestId: number): void {
    // Validate request ID before opening modal
    if (!requestId || requestId <= 0) {
      this.errorMsg = 'Invalid request ID. Cannot view reorder options.';
      return;
    }

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
      this.errorMsg = 'Invalid request ID. Cannot edit request.';
      return;
    }
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

  // ── Counters ──────────────────────────────────────────────────────────────

  get pendingCount(): number {
    return this.requests.filter(r => {
      const s = this.normalizeStatus(r.status);
      return s === 'pendingwithissuer' || s === 'pendingadminapproval';
    }).length;
  }
  get requestedCount(): number { return this.requests.filter(r => this.normalizeStatus(r.status) === 'pendingwithissuer').length; }
  get issuedCount():    number { return this.requests.filter(r => this.normalizeStatus(r.status) === 'pendingadminapproval').length; }
  get approvedCount():  number { return this.requests.filter(r => this.normalizeStatus(r.status) === 'approved').length; }
  get receivedCount():  number { return this.requests.filter(r => this.normalizeStatus(r.status) === 'received').length; }
  get rejectedCount():  number {
    return this.requests.filter(r => {
      const s = this.normalizeStatus(r.status);
      return s === 'rejected' || s === 'notissued';
    }).length;
  }
}
