import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminPendingService, PendingUser } from './admin-pending.service';

@Component({
  selector: 'app-admin-pending',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-pending.html',
  styleUrls: ['./admin-pending.css']
})
export class AdminPendingComponent implements OnInit {
  pendingRequests: PendingUser[] = [];
  loading = false;
  loadingMore = false;
  errorMsg = '';
  successMsg = '';

  // Cursor pagination state
  limit = 10; // fixed page size
  afterId: number | null = null; // id of the last item fetched
  totalRecords = 0;

  // Track approval/rejection states
  approvingId: number | null = null;
  rejectingId: number | null = null;

  constructor(private adminPendingService: AdminPendingService) { }

  ngOnInit() {
    this.loadPendingRequests();
  }

  loadPendingRequests(append = false) {
    if (append) {
      this.loadingMore = true;
    } else {
      this.loading = true;
      this.afterId = null;
      this.pendingRequests = [];
    }
    this.errorMsg = '';

    this.adminPendingService.getPendingUsers(this.afterId, this.limit)
      .subscribe({
        next: (res) => {
          const fetched = res.data;
          if (append) {
            this.pendingRequests = [...this.pendingRequests, ...fetched];
          } else {
            this.pendingRequests = fetched;
          }
          // Update cursor based on last item's id
          if (fetched.length > 0) {
            this.afterId = fetched[fetched.length - 1].id;
          }
          this.totalRecords = res.totalRecords;
          this.loading = false;
          this.loadingMore = false;
        },
        error: (err: any) => {
          this.errorMsg = 'Unable to load pending registration requests.';
          this.loading = false;
          this.loadingMore = false;
          console.error('[ERROR] Failed to load pending users:', err);
        }
      });
  }

  loadMore() {
    if (this.afterId !== null) {
      this.loadPendingRequests(true);
    }
  }

  trackById(index: number, item: PendingUser) {
    return item.id;
  }

  // Use trackBy in ngFor to avoid unnecessary DOM re‑creation
  approve(id: number, roleId: number, departmentId: number) {
    if (!id || !roleId || !departmentId) {
      this.errorMsg = 'Invalid request data. Missing roleId or departmentId.';
      console.error('[ERROR] Invalid approval payload:', { id, roleId, departmentId });
      return;
    }

    this.approvingId = id;
    this.errorMsg = '';
    this.successMsg = '';

    const payload = { roleId, departmentId };
    console.log('[INFO] Submitting approval request:', {
      requestId: id,
      payload,
      timestamp: new Date().toISOString()
    });

    this.adminPendingService.approveUser(id, payload)
      .subscribe({
        next: (res: any) => {
          console.log('[✓] Approval successful:', {
            requestId: id,
            response: res,
            timestamp: new Date().toISOString()
          });

          this.successMsg = res.message || 'User approved successfully';
          this.removeRequestFromList(id);

          // Clear success message after 3 seconds
          setTimeout(() => {
            this.successMsg = '';
          }, 3000);

          // Optionally refresh the list after a short delay to ensure DB is updated
          setTimeout(() => {
            if (this.pendingRequests.length === 0) {
              this.loadPendingRequests();
            }
          }, 500);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message || 'Unable to approve the selected request.';
          console.error('[ERROR] Approval failed:', {
            requestId: id,
            error: err?.error,
            status: err?.status,
            timestamp: new Date().toISOString()
          });
        },
        complete: () => {
          this.approvingId = null;
        }
      });
  }

  reject(id: number) {
    if (!id) {
      this.errorMsg = 'Invalid request data.';
      console.error('[ERROR] Invalid rejection request:', { id });
      return;
    }

    this.rejectingId = id;
    this.errorMsg = '';
    this.successMsg = '';

    console.log('[INFO] Submitting rejection request:', {
      requestId: id,
      timestamp: new Date().toISOString()
    });

    this.adminPendingService.rejectUser(id)
      .subscribe({
        next: (res: any) => {
          console.log('[✓] Rejection successful:', {
            requestId: id,
            response: res,
            timestamp: new Date().toISOString()
          });

          this.successMsg = res.message || 'User rejected successfully';
          this.removeRequestFromList(id);

          // Clear success message after 3 seconds
          setTimeout(() => {
            this.successMsg = '';
          }, 3000);

          // Refresh list if needed
          setTimeout(() => {
            if (this.pendingRequests.length === 0) {
              this.loadPendingRequests();
            }
          }, 500);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message || 'Unable to reject the selected request.';
          console.error('[ERROR] Rejection failed:', {
            requestId: id,
            error: err?.error,
            status: err?.status,
            timestamp: new Date().toISOString()
          });
        },
        complete: () => {
          this.rejectingId = null;
        }
      });
  }

  removeRequestFromList(id: number) {
    this.pendingRequests = this.pendingRequests.filter(req => req.id !== id);
    this.totalRecords = Math.max(0, this.totalRecords - 1);
    if (this.pendingRequests.length > 0) {
      this.afterId = this.pendingRequests[this.pendingRequests.length - 1].id;
    } else {
      this.afterId = null;
    }
  }
}
