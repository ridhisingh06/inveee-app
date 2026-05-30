import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

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
  
  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.getMyRequests();
  }

  getMyRequests() {
    this.loading = true;
    this.http.get<any>(`${environment.apiUrl}/requests?pageNumber=${this.pageNumber}&pageSize=${this.pageSize}`)
      .subscribe({
        next: (res) => {
          this.requests = Array.isArray(res) ? res : (res.data ?? []);
          this.loading = false;
        },
        error: (err) => {
          console.error(err);
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

  getStatusClass(status: string): string {
    switch(this.normalizeStatus(status)) {
      case 'pendingwithissuer': return 'status-requested';
      case 'pendingadminapproval': return 'status-issued';
      case 'notissued': return 'status-not-issued';
      case 'requested': return 'status-requested';
      case 'pending': return 'status-pending';
      case 'approved': return 'status-approved';
      case 'issued': return 'status-issued';
      case 'rejected': return 'status-rejected';
      case 'received': return 'status-received';
      default: return '';
    }
  }

  getStatusLabel(status: string): string {
    switch (this.normalizeStatus(status)) {
      case 'pendingwithissuer': return 'Pending with Issuer';
      case 'pendingadminapproval': return 'Pending Admin Approval';
      case 'notissued': return 'Not Issued';
      case 'requested': return 'Pending with Issuer';
      case 'issued': return 'Pending Admin Approval';
      case 'approved': return 'Approved';
      case 'rejected': return 'Rejected';
      case 'received': return 'Received';
      default: return status;
    }
  }

  private normalizeStatus(status: string): string {
    return (status || '').toLowerCase();
  }
}
