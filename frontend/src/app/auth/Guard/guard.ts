import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/service';
import { Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {

  const auth = inject(AuthService);
  const router = inject(Router);

  console.log('[AuthGuard] URL:', state.url);
  console.log('[AuthGuard] isLoggedIn:', auth.isLoggedIn());

  // 🔐 Login check
  if (!auth.isLoggedIn()) {
    console.log('[AuthGuard] Not logged in, redirecting to login');
    router.navigate(['/login']);
    return false;
  }

  // ⏳ Expiration check
  if (auth.isTokenExpired()) {
    console.log('[AuthGuard] Token expired, redirecting to login');
    alert("Session expired. Please login again.");
    auth.logout();
    router.navigate(['/login']);
    return false;
  }

  const role = auth.getRole();
  console.log('[AuthGuard] Role:', role);

  // 🎭 Role check
  const allowedRoles = route.data?.['roles'];
  console.log('[AuthGuard] Allowed roles:', allowedRoles);

  if (allowedRoles && (!role || !allowedRoles.includes(role))) {
    console.log('[AuthGuard] Access denied - role mismatch or missing');
    alert("Access Denied");
    router.navigate(['/login']);
    return false;
  }

  console.log('[AuthGuard] Access granted');
  return true;
};
