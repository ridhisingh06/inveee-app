import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth/services/service';

@Component({
  selector: 'app-dashboard-redirect',
  standalone: true,
  template: `
    <div style="padding: 20px;">Redirecting...</div>
  `
})
export class DashboardRedirectComponent implements OnInit {
  constructor(private auth: AuthService, private router: Router) {}

  ngOnInit() {
    const role = this.auth.getRole();

    if (role === 'Admin') {
      this.router.navigate(['/admin-dashboard']);
      return;
    }

    if (role === 'User') {
      this.router.navigate(['/user-dashboard']);
      return;
    }

    if (role === 'Issuer') {
      this.router.navigate(['/issuer-dashboard']);
      return;
    }

    this.router.navigate(['/login']);
  }
}
