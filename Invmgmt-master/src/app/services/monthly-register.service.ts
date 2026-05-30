import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MonthlyRegisterResult } from '../models/monthly-register.model';

@Injectable({ providedIn: 'root' })
export class MonthlyRegisterService {
  private readonly base = `${environment.apiUrl}/admin/monthly-register`;

  constructor(private http: HttpClient) {}

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

    return this.http.get<MonthlyRegisterResult>(this.base, { params });
  }
}
