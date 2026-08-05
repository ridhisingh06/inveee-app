import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';

export interface RuntimeLogEntry {
  timestamp: Date;
  type: 'CLICK' | 'HTTP_REQUEST' | 'HTTP_RESPONSE' | 'ROUTER_EVENT';
  details: any;
}

@Injectable({
  providedIn: 'root',
})
export class RuntimeLoggerService {
  private logs: RuntimeLogEntry[] = [];

  log(entry: RuntimeLogEntry) {
    if (environment.enableRuntimeLogging) {
      this.logs.push(entry);
      console.log('[RuntimeLog]', entry);
    }
  }

  getLogs(): RuntimeLogEntry[] {
    return this.logs;
  }

  clearLogs(): void {
    this.logs = [];
  }

  exportLogsAsJson(): string {
    return JSON.stringify(this.logs, null, 2);
  }

  exportLogsAsCsv(): string {
    const header = ['timestamp', 'type', 'details'];
    const rows = this.logs.map(l => {
      const details = JSON.stringify(l.details).replace(/"/g, '""');
      return [l.timestamp.toISOString(), l.type, `"${details}"`].join(',');
    });
    return [header.join(','), ...rows].join('\n');
  }

  /**
   * Generate summary statistics for the collected logs.
   */
  getSummary() {
    const summary: any = {
      totalClicks: 0,
      duplicateClicks: 0,
      avgClickHandlerTimeMs: 0,
      totalHttpRequests: 0,
      duplicateHttpRequests: 0,
      failedHttpRequests: 0,
      status401: 0,
      status404: 0,
      status500: 0,
      slowRequests: 0,
      verySlowRequests: 0,
      totalRouterNavigations: 0,
      navigationCancellations: 0,
      navigationErrors: 0,
      avgNavigationDurationMs: 0,
      timeline: [] as string[],
    };

    const pendingHttp: Map<string, RuntimeLogEntry> = new Map();
    const navigationMap: Map<number, { start?: Date; end?: Date }> = new Map();
    const clickMap: Map<string, Date> = new Map();
    const clickDurations: number[] = [];

    // sort logs chronologically just in case
    const sorted = [...this.logs].sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());

    for (const entry of sorted) {
      const tsStr = entry.timestamp.toISOString();
      summary.timeline.push(`${tsStr} ${entry.type}`);
      switch (entry.type) {
        case 'CLICK':
          summary.totalClicks++;
          // identify element by innerText + classes
          const elemKey = `${entry.details.elementType || 'button'}|${entry.details.innerText}|${entry.details.classes}`;
          const prev = clickMap.get(elemKey);
          if (prev && entry.timestamp.getTime() - prev.getTime() <= 1000) {
            summary.duplicateClicks++;
          }
          clickMap.set(elemKey, entry.timestamp);
          // placeholder for handler execution time if provided
          if (entry.details.executionTime) {
            clickDurations.push(entry.details.executionTime);
          }
          break;
        case 'HTTP_REQUEST':
          summary.totalHttpRequests++;
          const key = `${entry.details.method}|${entry.details.url}`;
          if (pendingHttp.has(key)) {
            summary.duplicateHttpRequests++;
          }
          pendingHttp.set(key, entry);
          break;
        case 'HTTP_RESPONSE':
          const rKey = `${entry.details.method}|${entry.details.url}`;
          const req = pendingHttp.get(rKey);
          if (req) {
            const duration = entry.details.duration;
            if (duration > 500) summary.slowRequests++;
            if (duration > 1000) summary.verySlowRequests++;
            pendingHttp.delete(rKey);
          }
          const status = entry.details.status;
          if (status >= 400) summary.failedHttpRequests++;
          if (status === 401) summary.status401++;
          if (status === 404) summary.status404++;
          if (status === 500) summary.status500++;
          break;
        case 'ROUTER_EVENT':
          const ev = entry.details.event;
          if (ev === 'NavigationStart') {
            summary.totalRouterNavigations++;
            const id = entry.details.id;
            navigationMap.set(id, { start: entry.timestamp });
          } else if (ev === 'NavigationEnd') {
            const id = entry.details.id;
            const nav = navigationMap.get(id);
            if (nav && nav.start) {
              const dur = entry.timestamp.getTime() - nav.start.getTime();
              nav.end = entry.timestamp;
              // accumulate for average
              if (!summary._navDurations) summary._navDurations = [] as number[];
              summary._navDurations.push(dur);
            }
          } else if (ev === 'NavigationCancel') {
            summary.navigationCancellations++;
          } else if (ev === 'NavigationError') {
            summary.navigationErrors++;
          }
          break;
      }
    }

    // compute averages
    if (clickDurations.length) {
      summary.avgClickHandlerTimeMs = clickDurations.reduce((a, b) => a + b, 0) / clickDurations.length;
    }
    if (summary._navDurations && summary._navDurations.length) {
      summary.avgNavigationDurationMs = summary._navDurations.reduce((a, b) => a + b, 0) / summary._navDurations.length;
    }
    delete summary._navDurations;
    return summary;
  }
}
