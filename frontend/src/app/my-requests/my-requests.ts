import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { RefreshService } from '../services/refresh.service';
import { normalizeStatus, getStatusClass, getStatusLabel } from '../utils/status.util';
import { OrderReceiptComponent } from '../order-receipt/order-receipt.component';

@Component({
  standalone: true,
  selector: 'app-my-requests',
  imports: [CommonModule, OrderReceiptComponent],
  templateUrl: './my-requests.html',
  styleUrls: ['./my-requests.css']
})
export class MyRequestsComponent implements OnInit, OnDestroy {
  requests: any[] = [];
  pageNumber = 1;
  pageSize = 10;
  loading = false;
  errorMsg = '';
  normalizeStatus = normalizeStatus;
  getStatusClass = getStatusClass;
  getStatusLabel = getStatusLabel;

  selectedRequestId: number | null = null;
  showReceipt = false;

  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private refresh: RefreshService
  ) {}

  ngOnInit() {
    // ✅ Reload whenever another component (EditRequest, UserCart, etc.)
    //    signals that request data has changed.
    this.refresh.requests$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.getMyRequests());

    this.getMyRequests();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getMyRequests() {
    this.loading = true;
    this.errorMsg = '';

    this.http.get<any>(
      `${environment.apiUrl}/requests?pageNumber=${this.pageNumber}&pageSize=${this.pageSize}`
    ).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.requests = Array.isArray(res) ? res : (res.data ?? []);
          this.loading = false;
        },
        error: (err) => {
          console.error('[MyRequestsComponent] Error fetching requests:', err);
          this.errorMsg = err?.error?.message || 'Failed to load requests. Please try again.';
          this.loading = false;
        }
      });
  }

  viewReceipt(requestId: number) {
    this.selectedRequestId = requestId;
    this.showReceipt = true;
  }

  closeReceipt() {
    this.showReceipt = false;
    this.selectedRequestId = null;
  }

  canShowReceivedButton(status: string): boolean {
    return status === 'Approved' || status === 'Ready To Receive';
  }

  nextPage() {
    if (this.requests.length === this.pageSize) {
      this.pageNumber++;
      this.getMyRequests();
    }
  }

  prevPage() {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.getMyRequests();
    }
  }
}
