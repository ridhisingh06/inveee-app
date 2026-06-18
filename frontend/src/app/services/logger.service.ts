import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

export type LogLevel = 'debug' | 'log' | 'warn' | 'error';

/**
 * Centralised logging service for InvMgmt.
 *
 * - In production, DEBUG and LOG levels are suppressed.
 * - Each message is prefixed with [InvMgmt][context][ISO-timestamp].
 * - Inject this service wherever you need structured logging instead of
 *   raw console calls.
 */
@Injectable({ providedIn: 'root' })
export class LoggerService {
  private readonly isProd = environment.production;

  private format(level: LogLevel, context: string, message: string): string {
    const ts = new Date().toISOString();
    return `[InvMgmt][${context}][${ts}] ${level.toUpperCase()}: ${message}`;
  }

  debug(context: string, message: string, ...args: unknown[]): void {
    if (this.isProd) return;
    console.debug(this.format('debug', context, message), ...args);
  }

  log(context: string, message: string, ...args: unknown[]): void {
    if (this.isProd) return;
    console.log(this.format('log', context, message), ...args);
  }

  warn(context: string, message: string, ...args: unknown[]): void {
    console.warn(this.format('warn', context, message), ...args);
  }

  error(context: string, message: string, ...args: unknown[]): void {
    console.error(this.format('error', context, message), ...args);
  }
}
