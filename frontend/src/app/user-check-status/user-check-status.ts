import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { WorkflowService } from '../services/workflow.service';
import { CartService } from '../services/cart.service';
import { normalizeStatus, getStatusClass, getStatusLabel } from '../utils/status.util';
import { ReorderModalComponent } from './reorder-modal.component';

@Component({
  selector: 'app-user-check-status',
  standalone: true,
  imports: [CommonModule, RouterModule, ReorderModalComponent],
  templateUrl: './user-check-status.html',
  styleUrls: ['./user-check-status.css']
})
export class UserCheckStatusComponent implements OnInit {
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

  constructor(
    private http: HttpClient,
    private workflow: WorkflowService,
    private cart: CartService,
    private router: Router
  ) {}

  ngOnInit() { this.loadRequests(); }

  loadRequests() {
    this.loading  = true;
    this.errorMsg = '';
    // Use the role-aware GET /api/requests endpoint (USER sees own requests)
    this.http.get<any>(`${environment.apiUrl}/requests`)
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
      .subscribe({
        next: (res) => {
          // Mark request as received in the local list
          const req = this.requests.find((r: any) => r.id === requestId);
          if (req) {
            req.status = 'Received';
            (req.items ?? []).forEach((i: any) => { i.status = 'Received'; });
          }
          if (res.orderSummaryId) {
            this.orderSummaryMap[requestId] = res.orderSummaryId;
          }
          this.successMsg = `Request #${requestId} received! Order receipt generated.`;
          delete this.receivingMap[requestId];
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
      // Try to look up via request ID
      this.workflow.getOrderSummaryByRequestId(requestId).subscribe({
        next: (os) => this.router.navigate(['/user-dashboard/order-summary', os.id]),
        error: ()  => this.errorMsg = 'Order receipt not found for this request.'
      });
    }
  }

  // ── Reorder logic ────────────────────────────────────────────────────────

  openReorderModal(requestId: number): void {
    this.isReorderModalOpen = true;
    this.reorderLoading = true;
    this.reorderSuggestions = [];
    
    this.http.get<any[]>(`${environment.apiUrl}/requests/${requestId}/reorderable-items`)
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
      // Create a mock item object that matches what CartService expects.
      // CartService expects an Item model, which should at least have id and name.
      const mockItem = { id: item.itemId, name: item.itemName, stockLimit: 999, availableQuantity: 999, categoryId: 0, reorderLevel: 0 } as any;
      this.cart.addItem(mockItem, item.suggestedQuantity);
    });
    
    this.successMsg = 'Reorder items added to cart! Redirecting...';
    setTimeout(() => { 
      this.successMsg = ''; 
      this.router.navigate(['/user-dashboard/cart']);
    }, 1500);
  }

  // ── Edit Request ─────────────────────────────────────────────────────────

  /** A request is editable only if it is PendingWithIssuer and all items are still PendingWithIssuer */
  isEditable(req: any): boolean {
    if (this.normalizeStatus(req.status) !== 'pendingwithissuer') return false;
    if (!req.items || req.items.length === 0) return false;
    return req.items.every((i: any) => this.normalizeStatus(i.status) === 'pendingwithissuer');
  }

  editRequest(requestId: number): void {
    this.router.navigate(['/user-dashboard/edit-request', requestId]);
  }

  // ── Per-item receive (legacy — kept for backward compat) ─────────────────

  receiveItem(requestId: number, itemId: number) {
    this.http.patch(`${environment.apiUrl}/requests/${requestId}/items/${itemId}/receive`, {})
      .subscribe({
        next: () => {
          const req = this.requests.find((r: any) => r.id === requestId);
          if (req?.items) {
            const item = req.items.find((i: any) => i.id === itemId);
            if (item) item.status = 'Received';
            const allDone = req.items.every((i: any) => {
              const s = this.normalizeStatus(i.status);
              return s === 'received' || s === 'notissued' || s === 'rejected';
            });
            if (allDone) req.status = 'Received';
          }
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

  // getStatusClass and getStatusLabel are provided by imported utilities

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

  // normalizeStatus is provided by imported utility
}
