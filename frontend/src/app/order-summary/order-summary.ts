// OrderSummaryComponent — Professional Receipt Page with extensive debug logs
import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { WorkflowService } from '../services/workflow.service';
import { OrderSummary, OrderSummaryItem } from '../models/request.model';

/**
 * OrderSummaryComponent — Professional Receipt Page
 *
 * Displays the complete, immutable order summary (receipt) for a single order.
 * Route: /user-dashboard/order-summary/:id
 */
@Component({
  standalone: true,
  selector: 'app-order-summary',
  imports: [CommonModule, RouterModule],
  templateUrl: './order-summary.html',
  styleUrls: ['./order-summary.css']
})
export class OrderSummaryComponent implements OnInit, OnDestroy {
  // Unique identifier for this component instance (debugging)
  private readonly instanceId: string = Math.random().toString(36).slice(2);

  order: OrderSummary | null = null;
  loading = true;
  errorMsg = '';

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly workflow: WorkflowService
  ) {
    console.log('[OrderSummary]', this.instanceId, 'constructor');
  }

  ngOnInit(): void {
    console.log('[OrderSummary]', this.instanceId, 'ngOnInit');
    const id = Number(this.route.snapshot.paramMap.get('id'));
    console.log('[OrderSummary]', this.instanceId, 'route id =', id);
    if (!id) {
      this.errorMsg = 'Invalid order ID.';
      this.loading = false;
      console.log('[OrderSummary]', this.instanceId, 'invalid ID -> loading set false');
      return;
    }
    this.loadOrder(id);
  }

  ngOnDestroy(): void {
    console.log('[OrderSummary]', this.instanceId, 'ngOnDestroy');
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  private loadOrder(id: number): void {
    console.log('[OrderSummary]', this.instanceId, 'loadOrder start');
    console.log('[OrderSummary]', this.instanceId, 'BEFORE request', {
      loading: this.loading,
      order: this.order,
      errorMsg: this.errorMsg
    });
    this.workflow
      .getOrderSummaryById(id)
      .pipe(
        finalize(() => {
          console.log('[OrderSummary]', this.instanceId, 'FINALIZE - clearing loading state');
          this.loading = false;
        })
      )
      .subscribe({
        next: (data) => {
          try {
            console.log('[OrderSummary]', this.instanceId, 'SUCCESS RAW:', data);
            this.order = data;
            console.log('[OrderSummary]', this.instanceId, 'order assigned:', this.order);
            this.errorMsg = '';
            console.log('[OrderSummary]', this.instanceId, 'final success state:', {
              loading: this.loading,
              hasOrder: !!this.order,
              errorMsg: this.errorMsg
            });
          } catch (e) {
            console.error('[OrderSummary]', this.instanceId, 'ERROR WHILE PROCESSING SUCCESS RESPONSE:', e);
            this.loading = false;
            this.errorMsg = 'Failed to process order summary.';
          }
        },
        error: (err) => {
          console.error('[OrderSummary]', this.instanceId, 'API error:', err);
          this.errorMsg = err?.message || 'Failed to load order summary.';
          // loading will be cleared by finalize
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
