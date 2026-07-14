import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './user-dashboard.html',
  styleUrls: ['./user-dashboard.css']
})
export class UserDashboardComponent implements OnInit {
  sidebarOpen = true;
  userName     = '';
  userInitials = 'U';

  constructor(private router: Router) {}

  ngOnInit(): void {
    // Pull stored display name / email from auth token payload
    try {
      const token = localStorage.getItem('token');
      if (token) {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const name: string =
          payload['name'] ||
          payload['unique_name'] ||
          payload['email'] ||
          payload['sub'] ||
          '';
        this.userName     = name;
        this.userInitials = name
          .split(/[\s@.]+/)
          .filter(Boolean)
          .slice(0, 2)
          .map((w: string) => w[0].toUpperCase())
          .join('') || 'U';
      }
    } catch {
      // Token missing or malformed — no user card details shown
    }
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    this.router.navigate(['/login']);
  }
}
