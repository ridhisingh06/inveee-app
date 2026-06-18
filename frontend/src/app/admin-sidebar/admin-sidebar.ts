import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SidebarService } from '../services/sidebar.service';
import { ADMIN_NAV_GROUPS } from '../models/nav-constants';

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-sidebar.html',
  styleUrls: ['./admin-sidebar.css']
})
export class AdminSidebarComponent {
  get isSidebarCollapsed() {
    return this.sidebar.sidebarCollapsed();
  }

  constructor(private sidebar: SidebarService) {}

  readonly navGroups = ADMIN_NAV_GROUPS;
}
