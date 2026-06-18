import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AdminSidebarComponent } from '../admin-sidebar/admin-sidebar';
import { SidebarService } from '../services/sidebar.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, AdminSidebarComponent],
  template: `
    <div class="admin-shell" [class.sidebar-collapsed]="isSidebarCollapsed">
      <app-admin-sidebar></app-admin-sidebar>
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styleUrls: ['./admin-layout.css']
})
export class AdminLayoutComponent {
  get isSidebarCollapsed() {
    return this.sidebar.sidebarCollapsed();
  }

  constructor(private sidebar: SidebarService) {}
}
