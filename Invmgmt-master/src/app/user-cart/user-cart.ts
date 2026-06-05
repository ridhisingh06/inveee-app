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
  successMsg = '';
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
    this.checkCanRequest();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Check if current user can submit a request
   */
  private checkCanRequest() {
    this.http.get<{canRequest: boolean, message: string}>(`${environment.apiUrl}/requests/can-request`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.canRequest = res.canRequest;
          if (!this.canRequest) {
            this.errorMsg = res.message;
          }
        },
        error: (err) => {
          console.error('[UserCartComponent] Error checking request permission:', err);
          this.errorMsg = err?.error?.message || 'Unable to verify request permissions. Please try again.';
          this.canRequest = false;
        }
      });
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

  /**
   * Submit the cart as a new request
   */
  requestAll() {
    if (this.lines.length === 0) return;
    
    this.submitting = true;
    this.errorMsg = '';
    this.successMsg = '';

    // Transform cart lines into backend format
    const payload = {
      categoryId: null,
      items: this.lines.map(line => ({
        itemId: Number(line.item.id),
        quantity: line.qty
      }))
    };

    this.http.post(`${environment.apiUrl}/requests`, payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.successMsg = 'Request submitted successfully!';
          this.cart.clear();
          this.submitting = false;
          
          // Navigate after a short delay to show success message
          setTimeout(() => {
            this.router.navigate(['/user-dashboard/check-status']);
          }, 1500);
        },
        error: (err) => {
          console.error('[UserCartComponent] Error submitting request:', err);
          this.errorMsg = err?.error?.message || 'Failed to submit request. Please try again.';
          this.submitting = false;
        }
      });
  }
}
