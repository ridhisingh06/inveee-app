import {
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../auth/services/service';
import { CartService } from '../services/cart.service';
import { SidebarService } from '../services/sidebar.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent implements OnInit {
  role: string = '';
  username: string = '';
  cartCount = 0;
  get showSidebarToggle(): boolean {
    const adminRoutes = [
      'admin-dashboard',
      'pending-requests',
      'pending-approvals',
      'personnel-management',
      'stores-section-allocation',
      'incharge-allocation'
    ];
    return this.role === 'ADMIN' && adminRoutes.some(r => this.router.url.includes(r));
  }

  @Input() sidebarCollapsed = false;
  @Output() sidebarToggle = new EventEmitter<void>();

  constructor(
    private auth: AuthService,
    private cart: CartService,
    private router: Router,
    private sidebar: SidebarService,
    private destroyRef: DestroyRef,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.role = this.auth.getRole() ?? '';
    this.auth.role$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((r) => {
      this.role = r ?? '';
      this.cdr.detectChanges();
    });

    this.username = this.auth.getUsername() ?? '';
    this.auth.username$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((u) => {
      this.username = u ?? '';
      this.cdr.detectChanges();
    });

    this.cart.lines$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.cartCount = this.cart.getItemCountSnapshot();
      this.cdr.detectChanges();
    });
  }

  toggleSidebar() {
    this.sidebar.toggleSidebar();
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

