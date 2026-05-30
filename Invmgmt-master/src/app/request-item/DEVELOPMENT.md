# Request Item Module - Development Guide

## Component Architecture Diagram

```
RequestItemComponent (main)
├── ItemService
│   ├── getItems() → Observable<InventoryItem[]>
│   ├── searchItems()
│   ├── getItemsInStock()
│   └── Cache Management
├── RequestService
│   ├── createRequest()
│   ├── getMyRequests()
│   ├── getRequestById()
│   └── cancelRequest()
└── RequestDetailModalComponent (child)
    └── RequestService (shared)
```

## Data Flow

### 1. Item Loading Flow
```
Component.ngOnInit()
  ↓
ItemService.getItems()
  ↓
HTTP GET /api/inventory
  ↓
Cache in ItemService
  ↓
items$ BehaviorSubject
  ↓
Template renders item grid
```

### 2. Draft Management Flow
```
User clicks "Add"
  ↓
addToDraft(item)
  ↓
Check if item exists in draftItems[]
  ↓
If exists: increment quantity
If new: add with quantity = 1
  ↓
Template updates with ngFor
```

### 3. Request Submission Flow
```
User clicks "Submit Request"
  ↓
submitRequest()
  ↓
Create CreateRequestDto from draftItems
  ↓
RequestService.createRequest(dto)
  ↓
HTTP POST /api/requests
  ↓
Clear draftItems[]
  ↓
Show success & open detail modal
  ↓
RequestDetailModalComponent loads request
```

## Type System

### Request Status States
```typescript
Pending   → Initial state, awaiting approval
Approved  → Approved by admin/approver
Issued    → Items issued to user
Rejected  → Request rejected
Cancelled → User cancelled the request
```

### Item Lifecycle
```
InventoryItem (API)
    ↓
Add to Draft → DraftItem (with quantity)
    ↓
Submit Request → RequestItem (in Request)
```

## Reactive Forms Integration

The component uses Reactive Forms for search and filters:

```typescript
this.searchForm = this.fb.group({
  searchText: [''],    // Real-time search input
  category: ['']       // Category dropdown
});
```

**Why Reactive Forms?**
- Better form state management
- Easier validation (when needed)
- Proper TypeScript typing
- Easier to test
- Better for complex forms

## Observable Patterns Used

### 1. Async Pipe for Auto-unsubscribe
```html
<!-- Service properly manages subscriptions -->
<div *ngIf="loading$ | async">Loading...</div>
```

### 2. TakeUntil for Cleanup
```typescript
this.itemService
  .getItems()
  .pipe(takeUntil(this.destroy$))
  .subscribe(...);
```

### 3. DebounceTime for Performance
```typescript
searchForm.get('searchText')?.valueChanges
  .pipe(debounceTime(300))
  .subscribe(() => this.applyFilters());
```

## HTTP Error Handling Strategy

```
HTTP Request
    ↓
Error Occurs
    ↓
Service catches with catchError()
    ↓
Map error code to user message
    ↓
Set error$ BehaviorSubject
    ↓
Component displays error alert
    ↓
User can dismiss or retry
```

### Error Codes Handled
| Code | Meaning | Message |
|------|---------|---------|
| 400  | Bad Request | Invalid request data |
| 401  | Unauthorized | Please log in again |
| 403  | Forbidden | Access denied |
| 404  | Not Found | Item/Request not found |
| 409  | Conflict | Item already requested |
| 5xx  | Server Error | Server error, try again |

## Performance Considerations

### 1. Change Detection
- Uses OnPush strategy internally (trackBy)
- Only updates what changed in ngFor
- Efficient for large lists

### 2. Caching
- Items cached for 5 minutes
- Invalidate with `refreshCache()`
- Automatic refresh on manual refresh button

### 3. Pagination Ready
- Structure supports pagination (future enhancement)
- Can add page parameter to getItems()

### 4. Memory Management
- All subscriptions cleaned up in ngOnDestroy
- No memory leaks
- Proper resource disposal

## Security Considerations

### XSRF Protection
All POST/PATCH/DELETE requests include XSRF token via HttpClientXsrfModule

### Authorization
- Guard checks user roles before component loads
- Services check response for 401/403
- Automatic redirect to login on auth failure

### Input Validation
- Quantity validated in component before submit
- Server validates all inputs
- No SQL injection risk (parameterized queries on backend)

## Accessibility Features

### 1. ARIA Labels
```html
<input aria-label="Search items">
<button title="Add to draft">
```

### 2. Keyboard Navigation
- Tab through all controls
- Enter to submit
- Space to toggle filters
- Arrow keys in dropdowns

### 3. Screen Reader Support
- Semantic HTML structure
- Proper button labels
- Status badge descriptions
- Form field labels

### 4. Color Contrast
- WCAG AA compliant
- No color-only information
- Icons with text labels
- Clear visual hierarchy

## State Management

### Component State
```typescript
items: InventoryItem[]           // All loaded items
filteredItems: InventoryItem[]   // After search/filter
draftItems: DraftItem[]          // User's cart
```

### Service State
```typescript
// ItemService
loading$: BehaviorSubject<boolean>
error$: BehaviorSubject<string | null>
itemsCache$: Observable<InventoryItem[]>

// RequestService
loading$: BehaviorSubject<boolean>
error$: BehaviorSubject<string | null>
```

### UI State
```typescript
showDetailModal: boolean          // Modal visibility
selectedRequestId: number | null  // Which request to show
showStockOnly: boolean           // Filter toggle state
```

## Module Integration Example

### In app.routes.ts
```typescript
import { RequestItemComponent } from './request-item/request-item';

export const routes: Routes = [
  {
    path: 'request-item',
    component: RequestItemComponent,
    canActivate: [authGuard],
    data: { roles: ['User'] }
  }
];
```

### In component using RequestItemComponent
```typescript
// Already standalone, no module needed
// Just import component where used

import { RequestItemComponent } from './request-item/request-item';

@Component({
  imports: [RequestItemComponent],
  template: `<app-request-item></app-request-item>`
})
export class DashboardComponent {}
```

## Testing Strategy

### Services (Unit Tests)
1. Test API calls with HttpClientTestingModule
2. Test cache behavior
3. Test error handling
4. Test data transformation

### Component (Integration Tests)
1. Test data loading on init
2. Test filter/search functionality
3. Test draft operations
4. Test request submission
5. Test modal opening/closing

### Example Test
```typescript
it('should add item to draft and update total', () => {
  const item: InventoryItem = { id: 1, name: 'Laptop', /* ... */ };
  component.addToDraft(item);
  
  expect(component.draftItems.length).toBe(1);
  expect(component.getDraftTotal()).toBe(1);
  
  component.addToDraft(item);
  expect(component.draftItems[0].quantity).toBe(2);
});
```

## Common Tasks

### Add a New Filter
1. Add to ItemFilterOptions interface
2. Update filterItems() method in ItemService
3. Add UI control in template
4. Update applyFilters() in component
5. Test

### Add a New Column to Request Details
1. Add to RequestItem interface
2. Update API to return data
3. Add column to modal table
4. Add styling

### Change Request Submission Logic
1. Modify submitRequest() in component
2. Update CreateRequestDto if needed
3. Update backend validation
4. Test error cases

### Add Loading Skeleton
1. Create CSS for skeleton
2. Add skeleton HTML in template
3. Show when `loading$ | async`
4. Hide when data loaded

## Debugging Tips

### Check Loading State
```typescript
// In component
console.log(this.loading$);  // Check if loading

// In template
{{ (loading$ | async) | json }}
```

### Check Error State
```typescript
// In component
(error$ | async | json)

// In template
<div>{{ error$ | async }}</div>
```

### Network Debugging
```typescript
// Add in service
tap(data => console.log('API Response:', data))
```

### Subscription Debugging
```typescript
// Check if unsubscribed properly
private destroy$ = new Subject<void>();
ngOnDestroy() { this.destroy$.next(); }
```

## Production Checklist

- [ ] API endpoints configured correctly
- [ ] Error messages user-friendly
- [ ] Loading states visible
- [ ] No console errors/warnings
- [ ] Responsive design tested on mobile
- [ ] Accessibility tested with screen reader
- [ ] Performance tested with large datasets
- [ ] Cache duration appropriate
- [ ] XSRF protection enabled
- [ ] Authorization checks working
- [ ] Error tracking set up (Sentry/etc)
- [ ] Analytics events configured
- [ ] Documentation complete
- [ ] Team trained on maintenance

## Migration Guide from Old Component

### Old → New Mapping
| Old | New | Notes |
|-----|-----|-------|
| `HttpClient` | `ItemService` | Use service instead |
| `(ngModel)` | `formControlName` | Reactive forms |
| `any[]` | `InventoryItem[]` | Type safe |
| `draftRequest` | `draftItems` | Better naming |
| `filterItems()` | `applyFilters()` | More comprehensive |
| No error handling | Service errors | Proper error management |
| No caching | ItemService caching | Better performance |

### Migration Steps
1. Replace component import
2. Inject ItemService and RequestService
3. Update template bindings
4. Update component logic
5. Test thoroughly
