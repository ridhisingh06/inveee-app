import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthApiService } from '../services/auth-api.service';
import { AuthService } from '../services/service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent implements OnInit {
  email = '';
  password = '';
  errorMsg = '';
  successMsg = '';
  isLoading = false;
  isRetrying = false;
  retryCount = 0;
  maxRetries = 3;

  constructor(
    private authApi: AuthApiService,
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['successMsg']) {
        this.successMsg = params['successMsg'];
      }
    });
  }

  login() {
    // Validate input
    if (!this.email || !this.password) {
      this.errorMsg = 'Email and password are required';
      this.successMsg = '';
      console.warn('[WARN] Login attempt with missing credentials');
      return;
    }

    // Prevent multiple simultaneous requests
    if (this.isLoading) {
      console.warn('[WARN] Login already in progress');
      return;
    }

    this.retryCount = 0;
    this.performLogin();
  }

  private performLogin() {
    this.isLoading = true;
    this.errorMsg = '';
    this.successMsg = '';

    const payload = {
      email: this.email,
      password: this.password
    };

    console.log('[INFO] Submitting login request:', {
      email: this.maskEmail(this.email),
      attempt: this.retryCount + 1,
      timestamp: new Date().toISOString()
    });

    this.authApi.login(payload).subscribe({
      next: (res: any) => {
        this.handleLoginSuccess(res);
      },
      error: (err) => {
        this.handleLoginError(err);
      }
    });
  }

  private handleLoginSuccess(res: any) {
    console.log('[✓] Login API response received:', {
      hasToken: !!res?.token,
      message: res?.message,
      timestamp: new Date().toISOString()
    });

    this.errorMsg = '';

    if (!res?.token) {
      this.errorMsg = 'Login failed. Please try again.';
      this.isLoading = false;
      console.error('[ERROR] Login failed: No token in response');
      return;
    }

    // Clear localStorage to ensure fresh state
    localStorage.removeItem('token');
    localStorage.removeItem('role');

    // Persist auth state before routing
    this.auth.setToken(res.token);

    console.log('[✓] Token stored and auth state set');

    const role = this.auth.getRole();
    console.log('[INFO] User role extracted from token:', {
      role,
      timestamp: new Date().toISOString()
    });

    // Route based on role
    if (role === 'ADMIN' || role === 'Admin') {
      console.log('[✓] Routing to admin dashboard');
      this.router.navigate(['/admin-dashboard']);
    } else if (role === 'USER' || role === 'User') {
      console.log('[✓] Routing to user dashboard');
      this.router.navigate(['/user-dashboard']);
    } else if (role === 'ISSUER' || role === 'Issuer') {
      console.log('[✓] Routing to issuer dashboard');
      this.router.navigate(['/issuer-dashboard']);
    } else {
      console.warn('[WARN] Unknown role, redirecting to login:', { role });
      this.router.navigate(['/login']);
    }

    this.isLoading = false;
  }

  private handleLoginError(err: any) {
    this.isLoading = false;

    console.error('[ERROR] Login failed:', {
      status: err?.status,
      message: err?.error?.message || err?.error,
      email: this.maskEmail(this.email),
      timestamp: new Date().toISOString()
    });

    // Handle 502 Bad Gateway with retry logic
    if (err?.status === 502 || err?.status === 503 || err?.status === 504) {
      if (this.retryCount < this.maxRetries) {
        this.retryCount++;
        const delayMs = 1000 * this.retryCount; // Exponential backoff: 1s, 2s, 3s
        
        console.warn(`[WARN] Server error (${err?.status}). Retrying in ${delayMs}ms... (attempt ${this.retryCount}/${this.maxRetries})`);
        this.errorMsg = `Server temporarily unavailable. Retrying... (${this.retryCount}/${this.maxRetries})`;
        this.isRetrying = true;
        
        setTimeout(() => {
          this.isRetrying = false;
          this.performLogin();
        }, delayMs);
      } else {
        this.errorMsg = 'Server is currently unavailable. Please try again later.';
        console.error('[ERROR] Max retries exceeded for server error');
      }
      return;
    }

    // Network error (status 0)
    if (err?.status === 0) {
      this.errorMsg = 'Cannot reach server. Please check your connection.';
      return;
    }

    // Account not approved (403)
    if (err?.status === 403) {
      this.errorMsg = 'Your account is not approved yet.';
      console.info('[INFO] Login blocked: Pending approval');
      return;
    }

    // Validation/Auth errors (400, 401)
    if (err?.status === 401 || err?.status === 400) {
      const msg = err?.error?.message || err?.error || '';
      const normalizedMsg = String(msg).toLowerCase();
      
      if (normalizedMsg.includes('pending') || 
          normalizedMsg.includes('approval') ||
          normalizedMsg.includes('not approved')) {
        this.errorMsg = 'Your account is not approved yet.';
        console.info('[INFO] Login blocked: Account not approved');
      } else if (normalizedMsg.includes('invalid credentials')) {
        this.errorMsg = 'Invalid email or password. Please try again.';
        console.info('[INFO] Login failed: Invalid credentials');
      } else {
        this.errorMsg = msg || 'Login failed. Please try again.';
      }
      return;
    }

    // Server error (500)
    if (err?.status === 500) {
      this.errorMsg = 'Server error. Please try again later.';
      console.error('[ERROR] Server error during login');
      return;
    }

    // Generic error
    this.errorMsg = err?.error?.message || err?.error || 'Login failed. Please try again.';
  }

  /**
   * Masks email for safe logging (prevents exposing full PII in logs)
   */
  private maskEmail(email: string): string {
    if (!email) return '<empty>';

    const parts = email.split('@');
    if (parts.length !== 2) return '***';

    const local = parts[0];
    const domain = parts[1];

    if (local.length <= 1) return '*@' + domain;
    return local[0] + '***@' + domain;
  }
}
