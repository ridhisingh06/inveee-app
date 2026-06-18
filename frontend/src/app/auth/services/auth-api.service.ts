import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LoginPayload {
  email: string;
  password: string;
}

export interface RegisterPayload {
  username: string;
  email: string;
  password: string;
  designation: string;
  departmentId: number;
  roleId: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthApiService {
  constructor(private http: HttpClient) {}

  register(payload: RegisterPayload): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/auth/register`, payload);
  }

  login(payload: LoginPayload): Observable<{ token: string; role: string; message: string }> {
    return this.http.post<{ token: string; role: string; message: string }>(`${environment.apiUrl}/auth/login`, payload);
  }
}
