import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  RequestSummary,
  RequestDetail,
  CreateRequestPayload
} from '../models/request.model';

@Injectable({ providedIn: 'root' })
export class RequestService {
  private readonly base = `${environment.apiUrl}/requests`;

  constructor(private http: HttpClient) {}

  /**
   * Get the current user's requests (paginated)
   * @param page - Page number (1-based)
   * @param size - Items per page
   * @returns Observable<RequestSummary[]> - List of requests or error
   */
  getMyRequests(page = 1, size = 20): Observable<RequestSummary[]> {
    const params = new HttpParams()
      .set('pageNumber', page)
      .set('pageSize', size);
    
    return this.http.get<RequestSummary[]>(`${this.base}`, { params })
      .pipe(
        catchError(err => {
          console.error('[RequestService] Error fetching requests:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to load requests. Please try again.'
          ));
        })
      );
  }

  /**
   * Get full details (with items) for one request
   * @param id - Request ID
   * @returns Observable<RequestDetail> - Full request details or error
   */
  getRequestById(id: number): Observable<RequestDetail> {
    return this.http.get<RequestDetail>(`${this.base}/${id}`)
      .pipe(
        catchError(err => {
          console.error('[RequestService] Error fetching request details:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to load request details. Please try again.'
          ));
        })
      );
  }

  /**
   * Submit a new request from the shopping cart
   * @param payload - Request payload with items
   * @returns Observable<{ id: number; message: string }> - Creation result or error
   */
  createRequest(payload: CreateRequestPayload): Observable<{ id: number; message: string }> {
    return this.http.post<{ id: number; message: string }>(this.base, payload)
      .pipe(
        catchError(err => {
          console.error('[RequestService] Error creating request:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to create request. Please try again.'
          ));
        })
      );
  }

  /**
   * Mark a request as received by the user
   * @param id - Request ID
   * @returns Observable<{ message: string }> - Operation result or error
   */
  confirmReceived(id: number): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.base}/${id}/receive`, {})
      .pipe(
        catchError(err => {
          console.error('[RequestService] Error confirming receipt:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to confirm receipt. Please try again.'
          ));
        })
      );
  }

  /**
   * Cancel / delete a request (only Requested or Pending)
   * @param id - Request ID
   * @returns Observable<{ message: string }> - Operation result or error
   */
  cancelRequest(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.base}/${id}`)
      .pipe(
        catchError(err => {
          console.error('[RequestService] Error canceling request:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to cancel request. Please try again.'
          ));
        })
      );
  }

  /**
   * Check if the current user can submit a new request
   * @returns Observable<{ canRequest: boolean; message: string }> - Permission check or error
   */
  canRequest(): Observable<{ canRequest: boolean; message: string }> {
    return this.http.get<{ canRequest: boolean; message: string }>(`${this.base}/can-request`)
      .pipe(
        catchError(err => {
          console.error('[RequestService] Error checking request permission:', err);
          return throwError(() => new Error(
            err?.error?.message || 'Failed to check request permission. Please try again.'
          ));
        })
      );
  }
}
