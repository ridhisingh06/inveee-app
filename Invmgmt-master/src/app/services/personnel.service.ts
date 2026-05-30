import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PersonnelPagedResult } from '../models/personnel.model';

@Injectable({ providedIn: 'root' })
export class PersonnelService {
  private readonly base = `${environment.apiUrl}/personnel`;

  constructor(private http: HttpClient) {}

  getPersonnel(page = 1, pageSize = 20): Observable<PersonnelPagedResult> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PersonnelPagedResult>(this.base, { params });
  }

  deletePersonnel(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.base}/${id}`);
  }
}
