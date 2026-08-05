import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './auth/services/auth-interceptor';
import { httpLoggerInterceptor } from './http-logger.interceptor';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors(
        (function() {
          const interceptors = [authInterceptor];
          if (environment.enableRuntimeLogging) {
            interceptors.push(httpLoggerInterceptor);
          }
          return interceptors;
        })()
      )
    )
  ]
};
