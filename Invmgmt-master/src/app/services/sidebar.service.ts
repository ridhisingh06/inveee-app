import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SidebarService {
  sidebarCollapsed = signal(false);

  toggleSidebar() {
    this.sidebarCollapsed.set(!this.sidebarCollapsed());
  }

  closeSidebar() {
    this.sidebarCollapsed.set(true);
  }

  openSidebar() {
    this.sidebarCollapsed.set(false);
  }
}
