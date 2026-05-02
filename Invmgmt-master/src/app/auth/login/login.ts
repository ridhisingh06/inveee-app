import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';     
import { RouterModule } from '@angular/router';
import { AuthApiService } from '../services/auth-api.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent {

  email = '';
  password = '';

  errorMsg = '';

  constructor(private authApi: AuthApiService, private router: Router) { }

  login() {
    if (!this.email || !this.password) {
      this.errorMsg = 'Email and password are required';
      return;
    }

    const payload = {
      email: this.email,
      password: this.password
    };

    this.authApi.login(payload)
      .subscribe({
        next: (res: any) => {
          this.errorMsg = '';

          if (!res?.token) {
            this.errorMsg = 'Login failed. Please try again.';
            return;
          }

          // 🔐 token save
          localStorage.setItem('token', res.token);

          // 🔥 role extract
          const payloadDecoded = JSON.parse(atob(res.token.split('.')[1]));

          const role =
            payloadDecoded['role'] ||
            payloadDecoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

          localStorage.setItem('role', role);

          // 🚀 redirect
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          if (err?.status === 0) {
            this.errorMsg = 'Cannot reach server. Please check your connection.';
            return;
          }

          if (err?.status === 401) {
            this.errorMsg = 'Invalid credentials';
            return;
          }

          if (err?.status === 500) {
            this.errorMsg = 'Server error. Please try again later.';
            return;
          }

          this.errorMsg = err?.error?.message || err?.error || 'Login failed';
        }
      });
  }
}
