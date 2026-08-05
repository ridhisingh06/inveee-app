import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { RuntimeLoggerService } from './runtime-logger.service';

@Injectable()
export class HttpLoggerInterceptor implements HttpInterceptor {
  constructor(private logger: RuntimeLoggerService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const start = Date.now();
    this.logger.log({
      timestamp: new Date(),
      type: 'HTTP_REQUEST',
      details: {
        method: req.method,
        url: req.urlWithParams,
        headers: req.headers.keys()
      }
    });
    return next.handle(req).pipe(
      tap(
        event => {
          this.logger.log({
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
        error => {
          this.logger.log({
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
      )
    );
  }
}
