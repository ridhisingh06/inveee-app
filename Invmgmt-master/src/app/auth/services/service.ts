import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  // Token
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  // Decode JWT
  private decodeToken(): any {
    const token = this.getToken();
    if (!token) return null;

    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch {
      return null;
    }
  }

  //  Role
  getRole(): string |null {
    const payload = this.decodeToken();

    if (!payload) return null;

    return payload.role ||
      payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
      null;
  }
  //  Helpers (OUTSIDE getRole)
  isAdmin(): boolean {
    return this.getRole() === 'Admin';
  }

  isUser(): boolean {
    return this.getRole() === 'User';
  }

  //  Login check
  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  //  Logout
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('role'); // 🔥 extra safe
  }
}
