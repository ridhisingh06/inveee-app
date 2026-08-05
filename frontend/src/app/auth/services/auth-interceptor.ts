import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');
  const router = inject(Router);

  console.log('AuthInterceptor: request URL', req.url);
  console.log('AuthInterceptor: token present?', !!token);

  let newReq = req;
  if (token) {
    newReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    console.log('AuthInterceptor: added Authorization header');
  } else {
    console.log('AuthInterceptor: no token, not adding header');
  }
  console.log('AuthInterceptor: final headers', newReq.headers.keys());

  return next(newReq).pipe(
    catchError((error) => {
      if (error.status === 401) {
        // Token is expired or invalid
        localStorage.removeItem('token');
        localStorage.removeItem('role');
        router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
};
