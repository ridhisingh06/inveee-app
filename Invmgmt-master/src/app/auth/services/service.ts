import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { LoggerService } from '../../services/logger.service';

const CTX = 'AuthService';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly tokenSubject = new BehaviorSubject<string | null>(
    localStorage.getItem('token')
  );
  readonly token$ = this.tokenSubject.asObservable();

  private readonly roleSubject = new BehaviorSubject<string | null>(
    this.extractRole(this.tokenSubject.value)
  );
  readonly role$ = this.roleSubject.asObservable();

  constructor(private logger: LoggerService) {}

  setToken(token: string) {
    localStorage.setItem('token', token);
    this.tokenSubject.next(token);
    this.roleSubject.next(this.extractRole(token));
    this.logger.log(CTX, 'User logged in (token stored)');
  }

  getToken(): string | null {
    return this.tokenSubject.value;
  }

  getRole(): string | null {
    return this.roleSubject.value;
  }

  isAdmin(): boolean {
    return this.getRole() === 'ADMIN';
  }

  isUser(): boolean {
    return this.getRole() === 'USER';
  }

  isIssuer(): boolean {
    return this.getRole() === 'ISSUER';
  }

  isLoggedIn(): boolean {
    return !!this.tokenSubject.value;
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('role'); // extra safe (legacy key)
    this.tokenSubject.next(null);
    this.roleSubject.next(null);
    this.logger.log(CTX, 'User logged out');
  }

  private decodeJwtPayload(token: string): any | null {
    const part = token.split('.')[1];
    if (!part) {
      this.logger.warn(CTX, 'Attempted to decode invalid JWT structure');
      return null;
    }

    // JWT payload is base64url (not base64). Normalize + pad for atob().
    const normalized = part.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);

    try {
      return JSON.parse(atob(padded));
    } catch (err) {
      this.logger.warn(CTX, 'Failed to parse JWT payload', err);
      return null;
    }
  }

  private extractRole(token: string | null): string | null {
    if (!token) return null;
    const payload = this.decodeJwtPayload(token);
    if (!payload) return null;

    const role = (
      payload.role ||
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      null
    );

    return role ? role.toUpperCase() : null;
  }

  isTokenExpired(): boolean {
    const token = this.getToken();
    if (!token) return true;

    const payload = this.decodeJwtPayload(token);
    if (!payload || !payload.exp) {
       this.logger.warn(CTX, 'Token does not have an exp claim, assuming expired');
       return true;
    }

    // The exp claim is in seconds, Date.now() is in milliseconds.
    const expirationTimeMs = payload.exp * 1000;
    const expired = Date.now() >= expirationTimeMs;
    if (expired) {
      this.logger.warn(CTX, 'Token is expired');
    }
    return expired;
  }
}
