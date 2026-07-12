/**
 * Request Service
 * Handles all API communication for request-related operations
 * Provides strongly typed responses and error handling
 */

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  RequestDetail,
  RequestSummary,
  CreateRequestDto,
  RequestFilterOptions,
  PaginationParams
} from '../models/request.model';

@Injectable({
  providedIn: 'root'
})
export class RequestService {
  private readonly API_URL = `${environment.apiUrl}/requests`;
  private loading$ = new BehaviorSubject<boolean>(false);
  private error$ = new BehaviorSubject<string | null>(null);

  constructor(private http: HttpClient) {}

  /**
   * Get loading state
   */
  getLoading$(): Observable<boolean> {
    return this.loading$.asObservable();
  }

  /**
   * Get error state
   */
  getError$(): Observable<string | null> {
    return this.error$.asObservable();
  }

  /**
   * Clear error
   */
  clearError(): void {
    this.error$.next(null);
  }

  /**
   * Create a new request from cart
   */
  createRequest(dto: CreateRequestDto): Observable<{ id: number }> {
    this.setLoading(true);
    return this.http.post<{ id: number }>(`${this.API_URL}`, dto).pipe(
      tap(() => this.clearError()),
      catchError(error => this.handleError(error)),
      finalize(() => this.setLoading(false))
    );
  }

  /**
   * Get all user requests
   */
  getMyRequests(
    filters?: RequestFilterOptions,
    pagination?: PaginationParams
  ): Observable<RequestSummary[]> {
    this.setLoading(true);
    let params = new HttpParams();

    if (pagination) {
      params = params.set('pageNumber', pagination.page.toString());
      params = params.set('pageSize', pagination.pageSize.toString());
    }

    // GET /api/requests is role-based; for USER it returns the current user's requests.
    return this.http.get<RequestSummary[]>(`${this.API_URL}`, { params }).pipe(
      tap(data => this.applyClientFilters(data, filters)),
      catchError(error => this.handleError(error)),
      finalize(() => this.setLoading(false))
    );
  }

  /**
   * Get request details by ID
   */
  getRequestById(id: number): Observable<RequestDetail> {
    this.setLoading(true);
    return this.http.get<RequestDetail>(`${this.API_URL}/${id}`).pipe(
      catchError(error => this.handleError(error)),
      finalize(() => this.setLoading(false))
    );
  }

  /**
   * Cancel a request
   */
  cancelRequest(id: number): Observable<void> {
    this.setLoading(true);
    // Backend models cancellation as delete (only allowed while REQUESTED / legacy PENDING).
    return this.http.delete<void>(`${this.API_URL}/${id}`).pipe(
      tap(() => this.clearError()),
      catchError(error => this.handleError(error)),
      finalize(() => this.setLoading(false))
    );
  }

  /**
   * Delete a request (only if in draft/pending status)
   */
  deleteRequest(id: number): Observable<void> {
    this.setLoading(true);
    return this.http.delete<void>(`${this.API_URL}/${id}`).pipe(
      tap(() => this.clearError()),
      catchError(error => this.handleError(error)),
      finalize(() => this.setLoading(false))
    );
  }

  /**
   * Private helper: Set loading state
   */
  private setLoading(isLoading: boolean): void {
    this.loading$.next(isLoading);
  }

  /**
   * Private helper: Apply client-side filters
   */
  private applyClientFilters(data: RequestSummary[], filters?: RequestFilterOptions): RequestSummary[] {
    if (!filters || !data) return data;

    return data.filter(item => {
      if (filters.status && item.status !== filters.status) return false;
      if (filters.searchText) {
        const searchLower = (filters.searchText ?? '').toLowerCase();
        // Add more search criteria as needed
      }
      return true;
    });
  }

  /**
   * Private helper: Handle HTTP errors
   */
  private handleError(error: any): Observable<never> {
    let message = 'An error occurred';

    if (error.error instanceof ErrorEvent) {
      message = `Error: ${error.error.message}`;
    } else if (error.status === 400) {
      message = error.error?.message || 'Invalid request data';
    } else if (error.status === 401) {
      message = 'Unauthorized. Please log in again.';
    } else if (error.status === 403) {
      message = 'Access denied';
    } else if (error.status === 404) {
      message = 'Request not found';
    } else if (error.status === 409) {
      message = error.error?.message || 'Conflict: Item already requested';
    } else if (error.status >= 500) {
      message = 'Server error. Please try again later.';
    }

    this.error$.next(message);
    console.error('Request Service Error:', message, error);
    return throwError(() => new Error(message));
  }
}
