import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  standalone: true,
  selector: 'app-issuer-approved',
  imports: [CommonModule],
  templateUrl: './issuer-approved.html',
  styleUrls: ['./issuer-approved.css']
})
export class IssuerApprovedComponent implements OnInit {
  requests: any[] = [];
  loading = true;
  errorMsg = '';
  successMsg = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadApproved();
  }

  loadApproved() {
    this.loading = true;
    this.http.get<any>(`${environment.apiUrl}/admin/requests`)
      .subscribe({
        next: res => {
          const rows = Array.isArray(res) ? res : (res.data ?? []);
          this.requests = rows.filter((r: any) => (r.status === 'Approved' || r.status === 'APPROVED'));
          this.loading = false;
        },
        error: () => {
          this.errorMsg = 'Failed to load approved requests.';
          this.loading = false;
        }
      });
  }

  issueRequest(id: number) {
    this.successMsg = '';
    this.errorMsg = '';
    this.http.patch(`${environment.apiUrl}/requests/${id}/issue`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Request #${id} issued successfully!`;
          this.loadApproved();
        },
        error: err => {
          this.errorMsg = err?.error?.message || 'Failed to issue request.';
        }
      });
  }
}
