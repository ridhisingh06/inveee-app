import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { RuntimeLoggerService } from './runtime-logger.service';
import { inject } from '@angular/core';
import { Observable, tap } from 'rxjs';

/**
 * Functional HTTP logger interceptor.
 * Exported as `httpLoggerInterceptor` to match imports.
 */
export const httpLoggerInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
  const logger = inject(RuntimeLoggerService);
  const start = Date.now();
  logger.log({
    timestamp: new Date(),
    type: 'HTTP_REQUEST',
    details: {
      method: req.method,
      url: req.urlWithParams,
      headers: req.headers.keys()
    }
  });

  return next(req).pipe(
    tap({
      next: (event) => {
        logger.log({
          timestamp: new Date(),
          type: 'HTTP_RESPONSE',
          details: {
            url: req.urlWithParams,
            method: req.method,
            status: (event as any).status,
            duration: Date.now() - start
          }
        });
      },
      error: (error) => {
        logger.log({
          timestamp: new Date(),
          type: 'HTTP_RESPONSE',
          details: {
            url: req.urlWithParams,
            method: req.method,
            error: error.message,
            status: error.status,
            duration: Date.now() - start
          }
        });
      }
    })
  );
};
