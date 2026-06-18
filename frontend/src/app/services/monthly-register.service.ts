import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { MonthlyRegisterResult } from '../models/monthly-register.model';

@Injectable({ providedIn: 'root' })
export class MonthlyRegisterService {
  private readonly base = `${environment.apiUrl}/admin/monthly-register`;

  constructor(private http: HttpClient) {}

  /**
   * Get monthly register data with pagination and search
   * @param month - Month (1-12)
   * @param year - Year (e.g., 2026)
   * @param page - Page number (1-based)
   * @param pageSize - Items per page
   * @param search - Optional search term
   * @returns Observable<MonthlyRegisterResult> - Data or error
   */
  getMonthlyRegister(
    month: number,
    year: number,
    page = 1,
    pageSize = 20,
    search = ''
  ): Observable<MonthlyRegisterResult> {
    let params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString())
      .set('pageNumber', page.toString())
      .set('pageSize', pageSize.toString());

    if (search && search.trim().length > 0) {
      params = params.set('search', search.trim());
    }

    return this.http.get<MonthlyRegisterResult>(this.base, { params })
      .pipe(
        catchError(err => {
          console.error('[MonthlyRegisterService] Error fetching register:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to load monthly register. Please try again.'
          ));
        })
      );
  }
}
