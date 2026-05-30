/**
 * Request Detail Modal Component
 * Displays detailed information about a request in a modal
 * Shows request status, items, quantities, and approval information
 */

import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { RequestService } from '../../services/request.service';
import { RequestDetail, RequestStatus } from '../../models/request.model';

@Component({
  selector: 'app-request-detail-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './request-detail-modal.component.html',
  styleUrls: ['./request-detail-modal.component.css']
})
export class RequestDetailModalComponent implements OnInit, OnDestroy {
  @Input() requestId: number | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  request: RequestDetail | null = null;
  loading = false;
  error: string | null = null;

  private destroy$ = new Subject<void>();

  RequestStatus = RequestStatus;

  constructor(public requestService: RequestService) {}

  ngOnInit(): void {
    if (this.requestId && this.isOpen) {
      this.loadRequest();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load request details
   */
  public loadRequest(): void {
    if (!this.requestId) return;

    this.loading = true;
    this.error = null;

    this.requestService
      .getRequestById(this.requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe(
        (request: RequestDetail) => {
          this.request = request;
          this.loading = false;
        },
        (error) => {
          this.error = error.message || 'Failed to load request details';
          this.loading = false;
        }
      );
  }

  /**
   * Get status badge color
   */
  getStatusClass(status: RequestStatus): string {
    const statusMap: { [key in RequestStatus]: string } = {
      [RequestStatus.PendingWithIssuer]: 'status-requested',
      [RequestStatus.NotIssued]: 'status-not-issued',
      [RequestStatus.PendingAdminApproval]: 'status-issued',
      [RequestStatus.Requested]: 'status-requested',
      [RequestStatus.Pending]: 'status-pending',
      [RequestStatus.Issued]: 'status-issued',
      [RequestStatus.Approved]: 'status-approved',
      [RequestStatus.Rejected]: 'status-rejected',
      [RequestStatus.Received]: 'status-received',
      [RequestStatus.Cancelled]: 'status-cancelled'
    };
    return statusMap[status] || '';
  }

  getStatusLabel(status: RequestStatus): string {
    const statusMap: { [key in RequestStatus]: string } = {
      [RequestStatus.PendingWithIssuer]: 'Pending with Issuer',
      [RequestStatus.NotIssued]: 'Not Issued',
      [RequestStatus.PendingAdminApproval]: 'Pending Admin Approval',
      [RequestStatus.Requested]: 'Pending with Issuer',
      [RequestStatus.Pending]: 'Pending',
      [RequestStatus.Issued]: 'Pending Admin Approval',
      [RequestStatus.Approved]: 'Approved',
      [RequestStatus.Rejected]: 'Rejected',
      [RequestStatus.Received]: 'Received',
      [RequestStatus.Cancelled]: 'Cancelled'
    };
    return statusMap[status] || status;
  }

  /**
   * Format date for display
   */
  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  /**
   * Get approval percentage
   */
  getApprovalPercentage(): number {
    if (!this.request || !this.request.items.length) return 0;
    const totalRequested = this.request.items.reduce((sum, item) => sum + item.quantityRequested, 0);
    const totalApproved = this.request.items.reduce((sum, item) => sum + item.quantityApproved, 0);
    return totalRequested > 0 ? Math.round((totalApproved / totalRequested) * 100) : 0;
  }

  /**
   * Get issuance percentage
   */
  getIssuancePercentage(): number {
    if (!this.request || !this.request.items.length) return 0;
    const totalRequested = this.request.items.reduce((sum, item) => sum + item.quantityRequested, 0);
    const totalIssued = this.request.items.reduce((sum, item) => sum + item.quantityIssued, 0);
    return totalRequested > 0 ? Math.round((totalIssued / totalRequested) * 100) : 0;
  }

  /**
   * Close modal
   */
  closeModal(): void {
    this.close.emit();
  }

  /**
   * Cancel request (if allowed)
   */
  cancelRequest(): void {
    if (!this.request) return;

    if (confirm('Are you sure you want to cancel this request?')) {
      this.loading = true;
      this.requestService
        .cancelRequest(this.request.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe(
          () => {
            this.loadRequest();
          },
          (error) => {
            this.error = error.message || 'Failed to cancel request';
            this.loading = false;
          }
        );
    }
  }

  /**
   * Track by function
   */
  trackByItemId(index: number, item: any): number {
    return item.id;
  }
}
