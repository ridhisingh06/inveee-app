import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { WorkflowService } from '../services/workflow.service';
import { OrderSummary, OrderSummaryItem } from '../models/request.model';

/**
 * OrderSummaryComponent — Professional Receipt Page
 *
 * Displays the complete, immutable order summary (receipt) for a single order.
 *
 * Route: /user-dashboard/order-summary/:id  (order summary ID)
 *
 * Features:
 *  - Company header
 *  - Request metadata (IDs, dates, actors)
 *  - Item table with all quantity stages
 *  - Totals row
 *  - Print button (window.print)
 *  - Download PDF (print-to-PDF via browser)
 *  - Back button → order history
 */
@Component({
  standalone: true,
  selector: 'app-order-summary',
  imports: [CommonModule, RouterModule],
  templateUrl: './order-summary.html',
  styleUrls: ['./order-summary.css']
})
export class OrderSummaryComponent implements OnInit, OnDestroy {
  order: OrderSummary | null = null;
  loading  = true;
  errorMsg = '';

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly route:    ActivatedRoute,
    private readonly router:   Router,
    private readonly workflow: WorkflowService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMsg = 'Invalid order ID.';
      this.loading  = false;
      return;
    }
    this.loadOrder(id);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  private loadOrder(id: number): void {
    this.workflow.getOrderSummaryById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.order   = data;
          this.loading = false;
        },
        error: (err) => {
          this.errorMsg = err.message || 'Failed to load order summary.';
          this.loading  = false;
        }
      });
  }

  // ── Computed helpers ──────────────────────────────────────────────────────

  get totalReceived(): number {
    return (this.order?.items ?? []).reduce((s, i) => s + i.receivedQuantity, 0);
  }

  get totalIssuerRejected(): number {
    return (this.order?.items ?? []).reduce((s, i) => s + i.issuerRejectedQuantity, 0);
  }

  get totalAdminRejected(): number {
    return (this.order?.items ?? []).reduce((s, i) => s + i.adminRejectedQuantity, 0);
  }

  // ── Actions ───────────────────────────────────────────────────────────────

  print(): void {
    window.print();
  }

  downloadPdf(): void {
    // Leverage the browser's built-in print-to-PDF
    window.print();
  }

  goBack(): void {
    this.router.navigate(['/user-dashboard/order-history']);
  }
}
