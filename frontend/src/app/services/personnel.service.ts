import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { PersonnelPagedResult } from '../models/personnel.model';

/**
 * Service responsible for Personnel CRUD operations.
 * Provides pagination, create, update, and delete methods.
 */
@Injectable({ providedIn: 'root' })
export class PersonnelService {
  private readonly base = `${environment.apiUrl}/personnel`;

  constructor(private http: HttpClient) {}

  /** Retrieve a paginated list of personnel. */
  getPersonnel(page = 1, pageSize = 20): Observable<PersonnelPagedResult> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http
      .get<PersonnelPagedResult>(this.base, { params })
      .pipe(
        catchError(err => {
          console.error('[PersonnelService] Error fetching personnel:', err);
          return throwError(() => new Error(err?.error?.message || 'Failed to load personnel. Please try again.'));
        })
      );
  }

  /** Delete a personnel record. */
  deletePersonnel(id: number): Observable<{ message: string }> {
    return this.http
      .delete<{ message: string }>(`${this.base}/${id}`)
      .pipe(
        catchError(err => {
          console.error('[PersonnelService] Error deleting personnel:', err);
          return throwError(() => new Error(err?.error?.message || 'Failed to delete personnel. Please try again.'));
        })
      );
  }

  /** Create a new personnel entry. */
  createPersonnel(data: FormData): Observable<any> {
    return this.http
      .post<any>(this.base, data)
      .pipe(
        catchError(err => {
          if (err.status === 409) {
            return throwError(() => new Error(err.error?.message || 'Duplicate record detected.'));
          }
          console.error('[PersonnelService] Error creating personnel:', err);
          return throwError(() => new Error(err.error?.message || 'Failed to create personnel.'));
        })
      );
  }

  /** Update an existing personnel entry. */
  updatePersonnel(id: number, data: FormData): Observable<any> {
    return this.http
      .put<any>(`${this.base}/${id}`, data)
      .pipe(
        catchError(err => {
          if (err.status === 409) {
            return throwError(() => new Error(err.error?.message || 'Duplicate record detected.'));
          }
          console.error('[PersonnelService] Error updating personnel:', err);
          return throwError(() => new Error(err.error?.message || 'Failed to update personnel.'));
        })
      );
  }
}
