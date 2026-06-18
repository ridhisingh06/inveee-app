import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';

export interface IssuedRequestItem {
  id: number;
  itemId: number;
  itemName: string;
  quantityRequested: number;
  quantityIssued: number;
  status: string;
}

export interface IssuedRequest {
  id: number;
  userId: number;
  username: string;
  email: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  items: IssuedRequestItem[];
}

export interface PaginatedRequests {
  data: IssuedRequest[];
  total: number;
  totalPages: number;
  currentPage: number;
}

@Injectable({
  providedIn: 'root'
})
export class RequestStateService {
  private pendingAdminSubject = new BehaviorSubject<PaginatedRequests>({
    data: [], total: 0, totalPages: 1, currentPage: 1
  });
  public pendingAdminRequests$ = this.pendingAdminSubject.asObservable();

  private pendingIssuerSubject = new BehaviorSubject<PaginatedRequests>({
    data: [], total: 0, totalPages: 1, currentPage: 1
  });
  public pendingIssuerRequests$ = this.pendingIssuerSubject.asObservable();

  private readonly ctx = 'RequestStateService';

  constructor(
    private http: HttpClient,
    private logger: LoggerService
  ) {}

  fetchPendingAdminRequests(page: number = 1, pageSize: number = 10, search: string = '') {
    const query = `${environment.apiUrl}/requests?status=PendingAdminApproval&page=${page}&pageSize=${pageSize}${search ? `&q=${encodeURIComponent(search)}` : ''}`;
    return this.http.get<any>(query).subscribe({
      next: (res) => {
        const data = res.data || [];
        this.pendingAdminSubject.next({
          data,
          total: res.total ?? data.length,
          totalPages: res.totalPages ?? 1,
          currentPage: page
        });
        this.logger.log(this.ctx, `Loaded ${data.length} pending admin request(s) (page ${page})`);
      },
      error: (err) => this.logger.error(this.ctx, 'Failed to load pending admin requests', err)
    });
  }

  fetchPendingIssuerRequests(page: number = 1, pageSize: number = 10, search: string = '') {
    const query = `${environment.apiUrl}/requests?status=PendingWithIssuer&page=${page}&pageSize=${pageSize}${search ? `&q=${encodeURIComponent(search)}` : ''}`;
    return this.http.get<any>(query).subscribe({
      next: (res) => {
        const data = res.data ?? res;
        this.pendingIssuerSubject.next({
          data,
          total: res.total ?? data.length,
          totalPages: res.totalPages ?? 1,
          currentPage: page
        });
        this.logger.log(this.ctx, `Loaded ${data.length} pending issuer request(s) (page ${page})`);
      },
      error: (err) => this.logger.error(this.ctx, 'Failed to load pending issuer requests', err)
    });
  }

  updateItemStatus(workflow: 'ADMIN' | 'ISSUER', requestId: number, requestItemId: number, newStatus: string) {
    const subject = workflow === 'ADMIN' ? this.pendingAdminSubject : this.pendingIssuerSubject;
    const current = subject.getValue();

    const updatedData = current.data.map(req => {
      if (req.id === requestId) {
        return {
          ...req,
          items: (req.items ?? []).map(item => 
            item.id === requestItemId ? { ...item, status: newStatus } : item
          )
        };
      }
      return req;
    });

    subject.next({ ...current, data: updatedData });
  }
}
