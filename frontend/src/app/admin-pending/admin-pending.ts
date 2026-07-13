import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AdminPendingService, PendingUser } from './admin-pending.service';
import { RefreshService } from '../services/refresh.service';

@Component({
  selector: 'app-admin-pending',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-pending.html',
  styleUrls: ['./admin-pending.css']
})
export class AdminPendingComponent implements OnInit, OnDestroy {
  pendingRequests: PendingUser[] = [];
  loading = false;
  loadingMore = false;
  errorMsg = '';
  successMsg = '';

  // Cursor pagination state
  limit = 10;
  afterId: number | null = null;
  totalRecords = 0;

  // Track approval/rejection states
  approvingId: number | null = null;
  rejectingId: number | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private adminPendingService: AdminPendingService,
    private refresh: RefreshService
  ) {}

  ngOnInit() {
    this.loadPendingRequests();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
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
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const fetched = res.data;
          if (append) {
            this.pendingRequests = [...this.pendingRequests, ...fetched];
          } else {
            this.pendingRequests = fetched;
          }
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

  trackById(_index: number, item: PendingUser) {
    return item.id;
  }

  approve(id: number, roleId: number, departmentId: number) {
    if (!id || !roleId || !departmentId) {
      this.errorMsg = 'Invalid request data. Missing roleId or departmentId.';
      return;
    }

    this.approvingId = id;
    this.errorMsg = '';
    this.successMsg = '';

    this.adminPendingService.approveUser(id, { roleId, departmentId })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          this.successMsg = res.message || 'User approved successfully';
          this.removeRequestFromList(id);

          // ✅ Signal AdminDashboardComponent to refresh its summary counts.
          this.refresh.notifyRegistration();

          setTimeout(() => { this.successMsg = ''; }, 3000);

          // Reload list if now empty to pick up any further pending items.
          if (this.pendingRequests.length === 0) {
            this.loadPendingRequests();
          }
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message || 'Unable to approve the selected request.';
          console.error('[ERROR] Approval failed:', err);
        },
        complete: () => {
          this.approvingId = null;
        }
      });
  }

  reject(id: number) {
    if (!id) {
      this.errorMsg = 'Invalid request data.';
      return;
    }

    this.rejectingId = id;
    this.errorMsg = '';
    this.successMsg = '';

    this.adminPendingService.rejectUser(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          this.successMsg = res.message || 'User rejected successfully';
          this.removeRequestFromList(id);

          // ✅ Signal AdminDashboardComponent to refresh its summary counts.
          this.refresh.notifyRegistration();

          setTimeout(() => { this.successMsg = ''; }, 3000);

          if (this.pendingRequests.length === 0) {
            this.loadPendingRequests();
          }
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message || 'Unable to reject the selected request.';
          console.error('[ERROR] Rejection failed:', err);
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
