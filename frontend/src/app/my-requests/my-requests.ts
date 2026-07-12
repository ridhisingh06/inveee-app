import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { normalizeStatus, getStatusClass, getStatusLabel } from '../utils/status.util';

@Component({
  standalone: true,
  selector: 'app-my-requests',
  imports: [CommonModule],
  templateUrl: './my-requests.html',
  styleUrls: ['./my-requests.css']
})
export class MyRequestsComponent implements OnInit {
  requests: any[] = [];
  pageNumber = 1;
  pageSize = 10;
  loading = false;
  errorMsg = '';
  normalizeStatus = normalizeStatus;
  getStatusClass = getStatusClass;
  getStatusLabel = getStatusLabel;
  
  private destroy$ = new Subject<void>();

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.getMyRequests();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Fetch requests from API with proper error handling
   */
  getMyRequests() {
    this.loading = true;
    this.errorMsg = '';
    
    this.http.get<any>(`${environment.apiUrl}/requests?pageNumber=${this.pageNumber}&pageSize=${this.pageSize}`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.requests = Array.isArray(res) ? res : (res.data ?? []);
          this.loading = false;
        },
        error: (err) => {
          console.error('[MyRequestsComponent] Error fetching requests:', err);
          this.errorMsg = err?.error?.message || 'Failed to load requests. Please try again.';
          this.loading = false;
        }
      });
  }

  nextPage() {
    if (this.requests.length === this.pageSize) {
      this.pageNumber++;
      this.getMyRequests();
    }
  }

  prevPage() {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.getMyRequests();
    }
  }

  // getStatusClass and getStatusLabel provided by shared util
}
