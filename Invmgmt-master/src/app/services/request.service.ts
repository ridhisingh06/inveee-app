import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
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

  /** Get the current user's requests (paginated) */
  getMyRequests(page = 1, size = 20): Observable<RequestSummary[]> {
    const params = new HttpParams()
      .set('pageNumber', page)
      .set('pageSize', size);
    return this.http.get<RequestSummary[]>(`${this.base}`, { params });
  }

  /** Get full details (with items) for one request */
  getRequestById(id: number): Observable<RequestDetail> {
    return this.http.get<RequestDetail>(`${this.base}/${id}`);
  }

  /** Submit a new request from the shopping cart */
  createRequest(payload: CreateRequestPayload): Observable<{ id: number; message: string }> {
    return this.http.post<{ id: number; message: string }>(this.base, payload);
  }

  /** Mark a request as received by the user */
  confirmReceived(id: number): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.base}/${id}/receive`, {});
  }

  /** Cancel / delete a request (only Requested or Pending) */
  cancelRequest(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.base}/${id}`);
  }

  /** Check if the current user can submit a new request */
  canRequest(): Observable<{ canRequest: boolean; message: string }> {
    return this.http.get<{ canRequest: boolean; message: string }>(`${this.base}/can-request`);
  }
}
