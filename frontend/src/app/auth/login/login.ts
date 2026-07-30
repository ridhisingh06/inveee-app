import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthApiService } from '../services/auth-api.service';
import { AuthService } from '../services/service';
import { LoggerService } from '../../services/logger.service';

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
  errorMessage = '';
  isLoading = false;
  isRetrying = false;
  retryCount = 0;
  maxRetries = 3;

  constructor(
    private authApi: AuthApiService,
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private logger: LoggerService
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['successMsg']) {
        this.successMsg = params['successMsg'];
      }
    });
  }

  async login() {
    // Call backend login API
    this.authApi.login({ email: this.email, password: this.password }).subscribe({
      next: (res: any) => {
        if (res?.token) {
          // Store token and update role via AuthService state
          this.auth.setToken(res.token);
          this.logger.log('Login', 'Token stored and role extracted');
          // Navigate to user dashboard (role-specific navigation can be added later)
          const role = this.auth.getRole();
          let target = '/user-dashboard';
          if (role === 'ADMIN') { target = '/admin-dashboard'; }
          else if (role === 'ISSUER') { target = '/issuer-dashboard'; }
          this.router.navigate([target]);
        } else {
          this.errorMessage = res?.message || 'Login failed: No token returned';
        }
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Login error';
        this.logger.error('Login', err);
      }
    });
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
    // Navigate to appropriate dashboard based on role
    const role = this.auth.getRole();
    let target = '/user-dashboard';
    if (role === 'ADMIN') { target = '/admin-dashboard'; }
    else if (role === 'ISSUER') { target = '/issuer-dashboard'; }
    this.router.navigate([target]);

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
