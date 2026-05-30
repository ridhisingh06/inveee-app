import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  SectionWiseQueryOfficer,
  SectionWiseQueryItem,
  SectionWiseQueryFilter,
  SectionWiseQueryResult
} from '../models/section-wise-query.model';

@Injectable({ providedIn: 'root' })
export class SectionWiseQueryService {
  private readonly base = `${environment.apiUrl}/admin/section-wise-query`;

  constructor(private http: HttpClient) {}

  getOfficers(): Observable<{ officers: SectionWiseQueryOfficer[] }> {
    return this.http.get<{ officers: SectionWiseQueryOfficer[] }>(`${this.base}/officers`);
  }

  getBhawans(): Observable<{ bhawans: string[] }> {
    return this.http.get<{ bhawans: string[] }>(`${this.base}/bhawans`);
  }

  searchItems(query: string): Observable<{ items: SectionWiseQueryItem[] }> {
    return this.http.get<{ items: SectionWiseQueryItem[] }>(`${this.base}/items/search`, {
      params: new HttpParams().set('query', query || '')
    });
  }

  getSectionWiseQuery(filter: SectionWiseQueryFilter): Observable<SectionWiseQueryResult> {
    let params = new HttpParams();

    if (filter.officerId != null) {
      params = params.set('officerId', filter.officerId.toString());
    }
    if (filter.fromDate) {
      params = params.set('fromDate', filter.fromDate);
    }
    if (filter.toDate) {
      params = params.set('toDate', filter.toDate);
    }
    if (filter.bhawan) {
      params = params.set('bhawan', filter.bhawan);
    }
    if (filter.itemId != null) {
      params = params.set('itemId', filter.itemId.toString());
    }
    if (filter.itemName) {
      params = params.set('itemName', filter.itemName);
    }
    params = params.set('pageNumber', (filter.pageNumber ?? 1).toString());
    params = params.set('pageSize', (filter.pageSize ?? 20).toString());

    return this.http.get<SectionWiseQueryResult>(this.base, { params });
  }

  exportCsv(filter: SectionWiseQueryFilter): Observable<Blob> {
    let params = new HttpParams();
    if (filter.officerId != null) params = params.set('officerId', filter.officerId.toString());
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter.toDate) params = params.set('toDate', filter.toDate);
    if (filter.bhawan) params = params.set('bhawan', filter.bhawan);
    if (filter.itemId != null) params = params.set('itemId', filter.itemId.toString());
    if (filter.itemName) params = params.set('itemName', filter.itemName);
    params = params.set('pageNumber', (filter.pageNumber ?? 1).toString());
    params = params.set('pageSize', (filter.pageSize ?? 10000).toString());

    return this.http.get(`${this.base}/export`, { params, responseType: 'blob' });
  }
}
