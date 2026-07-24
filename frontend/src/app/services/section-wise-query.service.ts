import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  SectionWiseQueryOfficer,
  SectionWiseQueryItem,
  SectionWiseQueryFilter,
  SectionWiseQueryResult
} from '../models/section-wise-query.model';

@Injectable({ providedIn: 'root' })
export class SectionWiseQueryService {
  private readonly base = `${environment.apiUrl}/admin/section-wise-query`;

  constructor(private http: HttpClient) {}

  /**
   * Get list of officers
   * @returns Observable<{ officers: SectionWiseQueryOfficer[] }> - Officers or error
   */
  getOfficers(): Observable<{ officers: SectionWiseQueryOfficer[] }> {
    return this.http.get<{ officers: SectionWiseQueryOfficer[] }>(`${this.base}/officers`)
      .pipe(
        catchError(err => {
          console.error('[SectionWiseQueryService] Error fetching officers:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to load officers. Please try again.'
          ));
        })
      );
  }

  /**
   * Get list of bhawans
   * @returns Observable<{ bhawans: string[] }> - Bhawans or error
   */
  getBhawans(): Observable<{ bhawans: string[] }> {
    return this.http.get<{ bhawans: string[] }>(`${this.base}/bhawans`)
      .pipe(
        catchError(err => {
          console.error('[SectionWiseQueryService] Error fetching bhawans:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to load bhawans. Please try again.'
          ));
        })
      );
  }

  /**
   * Search items
   * @param query - Search query string
   * @returns Observable<{ items: SectionWiseQueryItem[] }> - Search results or error
   */
  searchItems(query: string): Observable<{ items: SectionWiseQueryItem[] }> {
    return this.http.get<{ items: SectionWiseQueryItem[] }>(`${this.base}/items/search`, {
      params: new HttpParams().set('query', query || '')
    })
      .pipe(
        catchError(err => {
          console.error('[SectionWiseQueryService] Error searching items:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to search items. Please try again.'
          ));
        })
      );
  }

  /**
   * Get section-wise query results
   * @param filter - Filter parameters
   * @returns Observable<SectionWiseQueryResult> - Query results or error
   */
  getSectionWiseQuery(filter: SectionWiseQueryFilter): Observable<SectionWiseQueryResult> {
    let params = new HttpParams();

    if (filter.officerId != null) {
      params = params.set('officerId', filter.officerId.toString());
    }
    if (filter.fromDate) {
      params = params.set('fromDate', filter.fromDate);
    }
    if (filter.toDate) {
      params = params.set('toDate', filter.toDate);
    }
    if (filter.bhawan) {
      params = params.set('bhawan', filter.bhawan);
    }
    if (filter.itemCode != null) {
      params = params.set('itemCode', filter.itemCode.toString());
    }
    if (filter.itemName) {
      params = params.set('itemName', filter.itemName);
    }
    params = params.set('pageNumber', (filter.pageNumber ?? 1).toString());
    params = params.set('pageSize', (filter.pageSize ?? 20).toString());

    return this.http.get<SectionWiseQueryResult>(this.base, { params })
      .pipe(
        catchError(err => {
          console.error('[SectionWiseQueryService] Error fetching query results:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to fetch query results. Please try again.'
          ));
        })
      );
  }

  /**
   * Export query results as CSV
   * @param filter - Filter parameters
   * @returns Observable<Blob> - CSV file blob or error
   */
  exportCsv(filter: SectionWiseQueryFilter): Observable<Blob> {
    let params = new HttpParams();
    if (filter.officerId != null) params = params.set('officerId', filter.officerId.toString());
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter.toDate) params = params.set('toDate', filter.toDate);
    if (filter.bhawan) params = params.set('bhawan', filter.bhawan);
    if (filter.itemCode != null) params = params.set('itemCode', filter.itemCode.toString());
    if (filter.itemName) params = params.set('itemName', filter.itemName);
    params = params.set('pageNumber', (filter.pageNumber ?? 1).toString());
    params = params.set('pageSize', (filter.pageSize ?? 10000).toString());

    return this.http.get(`${this.base}/export`, { params, responseType: 'blob' })
      .pipe(
        catchError(err => {
          console.error('[SectionWiseQueryService] Error exporting CSV:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to export CSV. Please try again.'
          ));
        })
      );
  }
}
