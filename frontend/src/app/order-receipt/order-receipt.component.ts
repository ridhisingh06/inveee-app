import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RequestService } from '../services/request.service';
import { RefreshService } from '../services/refresh.service';

@Component({
  standalone: true,
  selector: 'app-order-receipt',
  imports: [CommonModule],
  templateUrl: './order-receipt.component.html',
  styleUrls: ['./order-receipt.component.css']
})
export class OrderReceiptComponent implements OnInit {
  @Input() requestId: number = 0;
  @Input() showReceivedButton: boolean = false;

  receipt: any = null;
  loading = false;
  error = '';
  isReceived = false;
  showNotification = false;
  notificationMessage = '';

  constructor(
    private requestService: RequestService,
    private refresh: RefreshService
  ) {}

  ngOnInit() {
    this.loadReceipt();
  }

  loadReceipt() {
    if (!this.requestId) return;

    this.loading = true;
    this.error = '';

    this.requestService.getOrderReceipt(this.requestId).subscribe({
      next: (data) => {
        this.receipt = data;
        this.isReceived = data.currentStatus === 'Received';
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load receipt';
        this.loading = false;
      }
    });
  }

  openConfirmationDialog() {
    if (confirm('Have you received all the approved items?')) {
      this.markAsReceived();
    }
  }

  markAsReceived() {
    this.requestService.confirmReceived(this.requestId).subscribe({
      next: () => {
        this.showNotification = true;
        this.notificationMessage = 'Items received successfully.';
        setTimeout(() => {
          this.showNotification = false;
        }, 3000);
        
        this.isReceived = true;
        this.showReceivedButton = false;
        this.refresh.notifyRequests();
        this.loadReceipt(); // Reload to show updated status
      },
      error: (err) => {
        this.showNotification = true;
        this.notificationMessage = err.message || 'Failed to mark as received';
        setTimeout(() => {
          this.showNotification = false;
        }, 5000);
      }
    });
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  formatDateTime(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
