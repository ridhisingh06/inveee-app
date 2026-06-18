import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PendingUser {
  id: number;
  username: string;
  email: string;
  role: string;
  status: string;
  roleId: number;
  departmentId: number;
  departmentName?: string;
  department?: string;
  createdAt?: string;
}

export interface PaginatedPendingResponse {
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  data: PendingUser[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminPendingService {
  constructor(private http: HttpClient) {}

  getPendingUsers(afterId: number | null = null, limit: number = 10): Observable<PaginatedPendingResponse> {
    const afterParam = afterId ? `&afterId=${afterId}` : '';
    return this.http.get<PaginatedPendingResponse>(
      `${environment.apiUrl}/admin/pending-users-cursor?limit=${limit}${afterParam}`
    );
  }

  approveUser(id: number, payload: { roleId: number; departmentId: number }): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${environment.apiUrl}/admin/approve/${id}`, payload);
  }

  rejectUser(id: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${environment.apiUrl}/admin/reject/${id}`, {});
  }
}
