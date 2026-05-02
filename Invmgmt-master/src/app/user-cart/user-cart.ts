import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CartService, CartLine } from '../services/cart.service';

@Component({
  selector: 'app-user-cart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-cart.html',
  styleUrls: ['./user-cart.css']
})
export class UserCartComponent {
  lines: CartLine[] = [];

  constructor(
    private cart: CartService,
    private router: Router
  ) {
    this.cart.lines$.subscribe((lines) => (this.lines = lines));
  }

  delete(itemId: string) {
    this.cart.removeItem(itemId);
  }

  async requestAll() {
    // TODO: wire to backend POST /api/requests etc.
    this.cart.clear();
    await this.router.navigate(['/user-dashboard', 'item-list']);
  }
}

