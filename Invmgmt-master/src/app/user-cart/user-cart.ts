import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CartService, CartLine } from '../services/cart.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-user-cart',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './user-cart.html',
  styleUrls: ['./user-cart.css']
})
export class UserCartComponent implements OnInit, OnDestroy {
  lines: CartLine[] = [];
  submitting = false;
  errorMsg = '';
  canRequest = true;

  private destroy$ = new Subject<void>();

  constructor(
    private cart: CartService,
    private router: Router,
    private http: HttpClient
  ) {
    // Subscribe with takeUntil to prevent memory leaks
    this.cart.lines$.pipe(takeUntil(this.destroy$)).subscribe((lines) => (this.lines = lines));
  }

  ngOnInit() {
    this.http.get<{canRequest: boolean, message: string}>(`${environment.apiUrl}/requests/can-request`)
      .subscribe({
        next: (res) => {
          this.canRequest = res.canRequest;
          if (!this.canRequest) {
            this.errorMsg = res.message;
          }
        },
        error: (err) => {
          console.error(err);
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Single method handles both +1 and -1 delta — avoids duplicate logic */
  changeQty(itemId: string | number, currentQty: number, delta: 1 | -1) {
    const next = currentQty + delta;
    if (next < 1) return;
    this.cart.updateQuantity(itemId, next);
  }

  delete(itemId: string | number) {
    this.cart.removeItem(itemId);
  }

  /** TrackBy prevents full list re-render; only the changed row updates */
  trackByItemId(_idx: number, line: CartLine): string | number {
    return line.item.id;
  }

  get totalUnits(): number {
    return this.lines.reduce((acc, line) => acc + line.qty, 0);
  }

  requestAll() {
    if (this.lines.length === 0) return;
    this.submitting = true;
    this.errorMsg = '';

    // Transform cart lines into backend format
    const payload = {
      categoryId: null, // Since we might have mixed categories, let backend handle it or set null
      items: this.lines.map(line => ({
        itemId: Number(line.item.id),
        quantity: line.qty
      }))
    };

    this.http.post(`${environment.apiUrl}/requests`, payload)
      .subscribe({
        next: () => {
          this.cart.clear();
          this.submitting = false;
          this.router.navigate(['/user-dashboard/check-status']);
        },
        error: (err) => {
          this.errorMsg = err?.error?.message || 'Failed to submit request. Please try again.';
          this.submitting = false;
        }
      });
  }
}
