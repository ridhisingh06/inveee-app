import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/service';
import { Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {

  const auth = inject(AuthService);
  const router = inject(Router);

  // 🔐 Login check
  if (!auth.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }

  // ⏳ Expiration check
  if (auth.isTokenExpired()) {
    alert("Session expired. Please login again.");
    auth.logout();
    router.navigate(['/login']);
    return false;
  }

  const role = auth.getRole();

  // 🎭 Role check
  const allowedRoles = route.data?.['roles'];

  if (allowedRoles && (!role || !allowedRoles.includes(role))) {
    alert("Access Denied");
    router.navigate(['/login']);
    return false;
  }

  return true;
};
