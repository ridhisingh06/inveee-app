# Developer Guide: Status Utilities & Component Patterns

This guide explains how to use the status normalization utilities and the implemented component patterns in your Angular application.

---

## Table of Contents
1. [Status Utility Overview](#status-utility-overview)
2. [Using Status Utilities in Components](#using-status-utilities-in-components)
3. [Component Patterns](#component-patterns)
4. [Common Use Cases](#common-use-cases)
5. [Testing Guidelines](#testing-guidelines)
6. [Troubleshooting](#troubleshooting)

---

## Status Utility Overview

### Location
```
src/app/utils/status.util.ts
```

### Available Functions

#### 1. `normalizeStatus(status: string | null | undefined): string`
Converts any backend status value to a normalized lowercase key for consistent comparisons.

**Why normalize?**
- Backend returns different formats: "PendingWithIssuer", "PENDING_WITH_ISSUER", "Pending With Issuer"
- Frontend needs consistent values for template comparisons
- Handles null/undefined gracefully
- Supports legacy data formats

**Examples:**
```typescript
normalizeStatus('PendingWithIssuer')     // → 'pendingwithissuer'
normalizeStatus('ISSUED')                // → 'issued' (legacy)
normalizeStatus('Approved')              // → 'approved'
normalizeStatus(null)                    // → ''
normalizeStatus(undefined)               // → ''
normalizeStatus('requested')             // → 'pendingwithissuer' (legacy alias)
```

#### 2. `getStatusLabel(status: string | null | undefined): string`
Returns a human-readable label for display in UI.

**Examples:**
```typescript
getStatusLabel('pendingwithissuer')      // → 'Pending with Issuer'
getStatusLabel('PendingAdminApproval')   // → 'Pending Admin Approval'
getStatusLabel('approved')               // → 'Approved'
getStatusLabel(null)                     // → 'Pending' (fallback)
```

#### 3. `getStatusClass(status: string | null | undefined): string`
Returns CSS class names for styling status badges.

**Examples:**
```typescript
getStatusClass('approved')               // → 'badge approved'
getStatusClass('rejected')               // → 'badge rejected'
getStatusClass('notissued')              // → 'badge not-issued'
```

---

## Using Status Utilities in Components

### Pattern 1: Template Helper Methods (Recommended for Templates)

```typescript
import { Component } from '@angular/core';
import { normalizeStatus, getStatusLabel, getStatusClass } from '../utils/status.util';

@Component({
  selector: 'app-my-component',
  template: `
    <div [class]="getStatusClass(item.status)">
      {{ getStatusLabel(item.status) }}
    </div>
  `
})
export class MyComponent {
  item = { status: 'Approved', name: 'Request #123' };

  // Expose utility functions to template
  normalizeStatus(status: string | null | undefined): string {
    return normalizeStatus(status);
  }

  getStatusLabel(status: string | null | undefined): string {
    return getStatusLabel(status);
  }

  getStatusClass(status: string | null | undefined): string {
    return getStatusClass(status);
  }
}
```

### Pattern 2: Direct Function Calls (In TypeScript)

```typescript
import { Component } from '@angular/core';
import { normalizeStatus, getStatusLabel } from '../utils/status.util';

@Component({
  selector: 'app-my-component',
  template: `<p>{{ displayLabel }}</p>`
})
export class MyComponent {
  item = { status: 'PendingWithIssuer' };
  displayLabel: string;

  ngOnInit() {
    // Use directly in TypeScript code
    const normalized = normalizeStatus(this.item.status);
    
    if (normalized === 'pendingwithissuer') {
      this.displayLabel = getStatusLabel(this.item.status);
    }
  }
}
```

### Pattern 3: Template Comparisons

```html
<!-- Compare normalized status -->
<ng-container *ngIf="normalizeStatus(item.status) === 'approved'">
  <button class="approve-btn">Action Complete</button>
</ng-container>

<!-- Use label for display -->
<span class="label">{{ getStatusLabel(item.status) }}</span>

<!-- Apply CSS classes -->
<span [class]="getStatusClass(item.status)">{{ item.status }}</span>
```

### Pattern 4: Map-based Status Tracking

Used in IssuerIssueComponent for tracking item status changes:

```typescript
export class MyComponent {
  // Track status changes without API call overhead
  statusMap: { [itemId: number]: string } = {};

  markAsIssued(itemId: number) {
    // Store normalized status for template comparisons
    this.statusMap[itemId] = 'pendingadminapproval';
    
    // In template:
    // <ng-container *ngIf="normalizeStatus(statusMap[item.id] || item.status) === 'pendingadminapproval'">
  }
}
```

---

## Component Patterns

### Pattern 1: API-Based Component with Loading States

**From: IssuerApprovedComponent**

```typescript
@Component({
  standalone: true,
  selector: 'app-issuer-approved',
  imports: [CommonModule],
  templateUrl: './issuer-approved.html'
})
export class IssuerApprovedComponent implements OnInit {
  requests: any[] = [];
  loading = true;
  errorMsg = '';
  successMsg = '';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadApproved();
  }

  loadApproved(): void {
    this.loading = true;
    this.errorMsg = '';
    
    this.http.get<any>(`${environment.apiUrl}/requests?status=Approved`)
      .subscribe({
        next: res => {
          // Flexible response handling
          this.requests = Array.isArray(res) ? res : (res.data ?? []);
          this.loading = false;
        },
        error: () => {
          this.errorMsg = 'Failed to load approved requests.';
          this.loading = false;
        }
      });
  }

  normalizeStatus(status: string | null | undefined): string {
    return normalizeStatus(status);
  }
}
```

**Key Points:**
- ✓ Handles both array and object responses
- ✓ Proper error handling with user-friendly messages
- ✓ Loading state management
- ✓ Exposes status utility to template

### Pattern 2: Reactive Component with Search and Pagination

**From: IssuerIssueComponent**

```typescript
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-issuer-issue',
  imports: [CommonModule, FormsModule],
  templateUrl: './issuer-issue.html'
})
export class IssuerIssueComponent implements OnInit, OnDestroy {
  requests: any[] = [];
  loading = true;
  errorMsg = '';
  successMsg = '';
  processingMap: { [itemId: number]: 'issuing' | 'rejecting' } = {};
  statusMap: { [itemId: number]: string } = {};

  searchText = '';
  currentPage = 1;
  pageSize = 10;
  total = 0;
  totalPages = 0;

  private readonly search$ = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly http: HttpClient,
    private readonly requestState: RequestStateService
  ) {}

  ngOnInit(): void {
    // Debounce search input
    this.search$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => this.loadRequests(1));

    // Subscribe to state updates
    this.requestState.pendingIssuerRequests$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state: PaginatedRequests) => {
        this.requests = state.data;
        this.total = state.total;
        this.totalPages = state.totalPages;
        this.currentPage = state.currentPage;
        this.loading = false;
      });

    this.loadRequests();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchChange(value: string): void {
    this.searchText = value;
    this.search$.next(value);
  }

  loadRequests(page = 1): void {
    this.loading = true;
    this.errorMsg = '';
    this.requestState.fetchPendingIssuerRequests(page, this.pageSize, this.searchText.trim());
  }

  issue(requestId: number, requestItemId: number): void {
    this.processingMap[requestItemId] = 'issuing';
    this.successMsg = '';
    this.errorMsg = '';

    this.http
      .patch(`${environment.apiUrl}/requests/${requestId}/items/${requestItemId}/issue`, {})
      .subscribe({
        next: () => {
          this.successMsg = `Item #${requestItemId} issued — pending admin approval.`;
          delete this.processingMap[requestItemId];
          this.statusMap[requestItemId] = 'pendingadminapproval';
          this.requestState.updateItemStatus('ISSUER', requestId, requestItemId, 'PendingAdminApproval');
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message ?? 'Failed to issue item.';
          delete this.processingMap[requestItemId];
        }
      });
  }

  normalizeStatus(status: string | null | undefined): string {
    return normalizeStatus(status);
  }
}
```

**Key Points:**
- ✓ Debounced search to reduce API calls
- ✓ Proper subscription cleanup with takeUntil
- ✓ Processing map to track UI state during async operations
- ✓ Status map caching to avoid flickering UI
- ✓ Reactive state management with services

### Pattern 3: Component with Approval Workflow

**From: AdminPendingComponent**

```typescript
@Component({
  selector: 'app-admin-pending',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-pending.html'
})
export class AdminPendingComponent implements OnInit {
  pendingRequests: PendingUser[] = [];
  loading = false;
  errorMsg = '';
  successMsg = '';

  approvingId: number | null = null;
  rejectingId: number | null = null;

  constructor(private adminPendingService: AdminPendingService) {}

  ngOnInit() {
    this.loadPendingRequests();
  }

  approve(id: number, roleId: number, departmentId: number) {
    // Validate required fields
    if (!id || !roleId || !departmentId) {
      this.errorMsg = 'Invalid request data. Missing roleId or departmentId.';
      return;
    }

    this.approvingId = id;
    const payload = { roleId, departmentId };

    this.adminPendingService.approveUser(id, payload)
      .subscribe({
        next: (res: any) => {
          this.successMsg = res.message || 'User approved successfully';
          this.removeRequestFromList(id);
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message || 'Unable to approve the selected request.';
        },
        complete: () => {
          this.approvingId = null;
        }
      });
  }

  removeRequestFromList(id: number) {
    this.pendingRequests = this.pendingRequests.filter(req => req.id !== id);
  }
}
```

**Key Points:**
- ✓ Validation before API calls
- ✓ Proper state management (approvingId tracks UI state)
- ✓ Complete callback for cleanup
- ✓ User feedback with success/error messages

---

## Common Use Cases

### Use Case 1: Display Status with Dynamic Styling

```html
<!-- Component -->
<span [class]="getStatusClass(item.status)" [title]="getStatusLabel(item.status)">
  {{ item.status }}
</span>

<!-- CSS -->
<style>
  .badge {
    padding: 4px 12px;
    border-radius: 4px;
    font-size: 12px;
    font-weight: 600;
  }
  
  .badge.approved {
    background-color: #d4edda;
    color: #155724;
  }
  
  .badge.rejected {
    background-color: #f8d7da;
    color: #721c24;
  }
  
  .badge.requested {
    background-color: #d1ecf1;
    color: #0c5460;
  }
</style>
```

### Use Case 2: Conditional Rendering Based on Status

```html
<!-- Show different UI based on normalized status -->
<ng-container [ngSwitch]="normalizeStatus(item.status)">
  
  <ng-container *ngSwitchCase="'approved'">
    <button (click)="markAsReceived(item.id)">Mark as Received</button>
  </ng-container>

  <ng-container *ngSwitchCase="'pendingadminapproval'">
    <button disabled>Awaiting Admin Approval</button>
  </ng-container>

  <ng-container *ngSwitchCase="'pendingwithissuer'">
    <button (click)="issue(item.id)">Issue Item</button>
    <button (click)="reject(item.id)">Reject</button>
  </ng-container>

  <ng-container *ngSwitchDefault>
    <span class="text-muted">{{ getStatusLabel(item.status) }}</span>
  </ng-container>

</ng-container>
```

### Use Case 3: Filter List by Status

```typescript
export class MyComponent {
  requests: any[] = [];
  selectedStatus = 'approved';

  get filteredRequests(): any[] {
    if (!this.selectedStatus) return this.requests;
    return this.requests.filter(req => 
      normalizeStatus(req.status) === this.selectedStatus
    );
  }
}
```

### Use Case 4: Track Item Status During Operations

```typescript
export class IssueItemComponent {
  items: any[] = [];
  processingMap: { [itemId: number]: 'issuing' | 'rejecting' } = {};
  statusMap: { [itemId: number]: string } = {};

  issueItem(itemId: number) {
    // Show loading state
    this.processingMap[itemId] = 'issuing';
    
    this.http.patch(`/api/items/${itemId}/issue`, {})
      .subscribe({
        next: () => {
          // Update status cache without API overhead
          this.statusMap[itemId] = 'issued';
          delete this.processingMap[itemId];
        },
        error: () => {
          delete this.processingMap[itemId];
        }
      });
  }

  isProcessing(itemId: number): boolean {
    return !!this.processingMap[itemId];
  }
}
```

```html
<button 
  (click)="issueItem(item.id)" 
  [disabled]="isProcessing(item.id)"
  [class.loading]="processingMap[item.id] === 'issuing'"
>
  {{ processingMap[item.id] === 'issuing' ? 'Issuing...' : 'Issue' }}
</button>
```

---

## Testing Guidelines

### Unit Test Example

```typescript
import { normalizeStatus, getStatusLabel, getStatusClass } from './status.util';

describe('Status Utility', () => {
  
  describe('normalizeStatus', () => {
    it('should normalize various status formats', () => {
      expect(normalizeStatus('PendingWithIssuer')).toBe('pendingwithissuer');
      expect(normalizeStatus('Approved')).toBe('approved');
      expect(normalizeStatus('approved')).toBe('approved');
    });

    it('should handle null and undefined', () => {
      expect(normalizeStatus(null)).toBe('');
      expect(normalizeStatus(undefined)).toBe('');
    });

    it('should handle legacy formats', () => {
      expect(normalizeStatus('requested')).toBe('pendingwithissuer');
      expect(normalizeStatus('issued')).toBe('pendingadminapproval');
    });
  });

  describe('getStatusLabel', () => {
    it('should return human-readable labels', () => {
      expect(getStatusLabel('approved')).toBe('Approved');
      expect(getStatusLabel('pendingwithissuer')).toBe('Pending with Issuer');
    });

    it('should fallback to pending for unknown status', () => {
      expect(getStatusLabel('unknown')).toBe('Pending');
    });
  });

  describe('getStatusClass', () => {
    it('should return appropriate CSS classes', () => {
      expect(getStatusClass('approved')).toContain('badge');
      expect(getStatusClass('approved')).toContain('approved');
    });
  });
});
```

### Component Test Example

```typescript
describe('IssuerApprovedComponent', () => {
  let component: IssuerApprovedComponent;
  let fixture: ComponentFixture<IssuerApprovedComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IssuerApprovedComponent, HttpClientTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(IssuerApprovedComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should load approved requests', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(req => 
      req.url.includes('status=Approved')
    );
    expect(req.request.method).toBe('GET');

    req.flush([
      { id: 1, status: 'Approved', itemName: 'Item 1' }
    ]);

    expect(component.requests.length).toBe(1);
    expect(component.loading).toBeFalse();
  });

  it('should normalize status in template', () => {
    component.requests = [
      { id: 1, status: 'PendingWithIssuer' }
    ];

    expect(component.normalizeStatus(component.requests[0].status))
      .toBe('pendingwithissuer');
  });
});
```

---

## Troubleshooting

### Issue 1: Status not displaying correctly

**Problem:** Status displays as empty or wrong value

**Solution:**
```typescript
// ❌ WRONG - Status value may be null/undefined
<span>{{ item.status }}</span>

// ✅ CORRECT - Use utility with null safety
<span>{{ getStatusLabel(item.status) }}</span>
```

### Issue 2: Template not updating after API call

**Problem:** UI doesn't reflect status change

**Solution:**
```typescript
// ✅ Use statusMap for immediate UI updates while API completes
this.statusMap[itemId] = 'approved';
this.http.patch(...).subscribe({
  next: () => {
    this.requestState.updateItemStatus(...);
  }
});
```

### Issue 3: Status comparisons not working

**Problem:** `*ngIf="item.status === 'approved'"` not working

**Solution:**
```html
<!-- ❌ WRONG - Case and format sensitive -->
<ng-container *ngIf="item.status === 'approved'">

<!-- ✅ CORRECT - Normalize first -->
<ng-container *ngIf="normalizeStatus(item.status) === 'approved'">
```

### Issue 4: Memory leaks from subscriptions

**Problem:** Component memory grows over time

**Solution:**
```typescript
// ✅ CORRECT - Unsubscribe pattern
private destroy$ = new Subject<void>();

ngOnInit() {
  this.observable$.pipe(
    takeUntil(this.destroy$)  // ← Unsubscribe on destroy
  ).subscribe(...);
}

ngOnDestroy() {
  this.destroy$.next();
  this.destroy$.complete();
}
```

---

## Best Practices Summary

1. **Always normalize status before comparisons**
   ```typescript
   normalizeStatus(status) === 'approved'  // ✓ Correct
   ```

2. **Use template methods for template access**
   ```typescript
   normalizeStatus(status: string | null | undefined): string {
     return normalizeStatus(status);
   }
   ```

3. **Handle null/undefined gracefully**
   ```typescript
   (status ?? '').toLowerCase()  // ✓ Safe
   ```

4. **Use trackBy in ngFor loops**
   ```html
   <tr *ngFor="let item of items; trackBy: trackById">
   ```

5. **Manage subscriptions with takeUntil**
   ```typescript
   .pipe(takeUntil(this.destroy$))
   ```

6. **Provide user feedback**
   ```typescript
   this.successMsg = 'Action completed';
   this.errorMsg = 'Something went wrong';
   ```

7. **Validate before API calls**
   ```typescript
   if (!id || !roleId) {
     this.errorMsg = 'Invalid data';
     return;
   }
   ```

---

## Quick Reference

| Function | Input | Output | Use Case |
|----------|-------|--------|----------|
| `normalizeStatus()` | `'PendingWithIssuer'` | `'pendingwithissuer'` | Comparisons in templates/code |
| `getStatusLabel()` | `'approved'` | `'Approved'` | Display in UI |
| `getStatusClass()` | `'approved'` | `'badge approved'` | Apply CSS classes |

---

**For questions or issues, refer to the component implementations in:**
- `src/app/issuer-approved/issuer-approved.ts`
- `src/app/issuer-issue/issuer-issue.ts`
- `src/app/admin-pending/admin-pending.ts`
