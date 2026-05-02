import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../auth/services/service';
import { CartService } from '../services/cart.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent implements OnInit {
  role: string = '';
  cartCount = 0;

  @Input() showSidebarToggle = false;
  @Output() sidebarToggle = new EventEmitter<void>();

  constructor(
    private auth: AuthService,
    private cart: CartService,
    private router: Router
  ) {}

  ngOnInit() {
    const token = localStorage.getItem('token');
    if (!token) return;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.role =
        payload['role'] ||
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
        '';
    } catch {
      this.role = '';
    }

    this.cart.lines$.subscribe(() => {
      this.cartCount = this.cart.getItemCountSnapshot();
    });
  }

  toggleSidebar() {
    this.sidebarToggle.emit();
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/']);
  }

  openCart() {
    this.router.navigate(['/user-dashboard', 'cart']);
  }
}
