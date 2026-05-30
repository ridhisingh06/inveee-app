# Request Item Module - Production Ready

## Overview

A completely refactored, production-ready Angular module for managing inventory requests. This module provides a clean, maintainable, and scalable solution for users to browse items, create draft requests, and submit them for approval.

## Key Improvements

### ✅ Architecture & Organization
- **Modular Structure**: Separated concerns into models, services, and components
- **Standalone Components**: Uses Angular 14+ standalone API
- **Type Safety**: Full TypeScript interfaces and enums for all data
- **Service-Based**: Proper separation of business logic into injectable services

### ✅ Features
- **Item Browsing**: Grid view with hover effects and visual feedback
- **Search & Filter**: Real-time search with debouncing, category filter, stock filter
- **Draft Management**: Quantity controls, add/remove items, clear draft
- **Request Submission**: Create requests from draft with validation
- **Request Details Modal**: View request status, items, and approval progress
- **Error Handling**: Comprehensive error messages and user feedback
- **Loading States**: Skeleton loading, spinners, and disabled states

### ✅ Code Quality
- **RxJS Best Practices**: Proper subscription management with `takeUntil`
- **Performance Optimization**: `trackBy` functions for `ngFor`, caching in ItemService
- **Accessibility**: ARIA labels, proper button types, keyboard navigation
- **Responsive Design**: Mobile-first approach, works on all screen sizes
- **Documentation**: JSDoc comments on all public methods

## Project Structure

```
request-item/
├── models/
│   └── request.model.ts           # All interfaces and enums
├── services/
│   ├── request.service.ts         # Request API operations
│   └── item.service.ts            # Item API operations & caching
├── components/
│   └── request-detail-modal/
│       ├── request-detail-modal.component.ts
│       ├── request-detail-modal.component.html
│       └── request-detail-modal.component.css
├── request-item.ts                # Main component
├── request-item.html              # Main template
└── request-item.css               # Main styles
```

## Core Components & Services

### Models (request.model.ts)

```typescript
// Key interfaces
- InventoryItem        // Single inventory item
- DraftItem           // Item with quantity
- RequestItem         // Item in a request
- RequestSummary      // Request list view
- RequestDetail       // Full request with items
- RequestStatus       // Enum: Pending, Approved, Issued, Rejected, Cancelled
```

### ItemService

**Responsibilities:**
- Fetch all inventory items
- Cache items for 5 minutes
- Search and filter items
- Handle API errors

**Key Methods:**
```typescript
getItems(forceRefresh?: boolean): Observable<InventoryItem[]>
searchItems(searchText: string, filters?: ItemFilterOptions): Observable<InventoryItem[]>
getItemsByCategory(categoryId: number): Observable<InventoryItem[]>
getItemsInStock(): Observable<InventoryItem[]>
refreshCache(): Observable<InventoryItem[]>
invalidateCache(): void
```

### RequestService

**Responsibilities:**
- Create requests from draft
- Fetch user requests
- Get request details
- Cancel/delete requests
- Handle API errors

**Key Methods:**
```typescript
createRequest(dto: CreateRequestDto): Observable<{ id: number }>
getMyRequests(filters?: RequestFilterOptions): Observable<RequestSummary[]>
getRequestById(id: number): Observable<RequestDetail>
cancelRequest(id: number): Observable<void>
deleteRequest(id: number): Observable<void>
```

### RequestItemComponent

**Features:**
- Load items on init
- Real-time search with debounce (300ms)
- Category filter
- Stock-only filter
- Draft management (add, remove, update quantity)
- Request submission
- Error display
- Loading states

**Key Methods:**
```typescript
loadItems()              // Fetch items from service
applyFilters()          // Apply all active filters
addToDraft(item)        // Add item to draft
removeFromDraft(id)     // Remove item from draft
updateDraftQuantity()   // Update item quantity
submitRequest()         // Create and submit request
refreshItems()          // Refresh cache
```

### RequestDetailModalComponent

**Features:**
- Display full request details
- Show request status with badges
- Approval and issuance progress bars
- Itemized breakdown table
- Cancel request capability
- Proper error handling
- Loading state management

## Usage Guide

### Basic Setup

1. **Import the component in your module/route:**

```typescript
import { RequestItemComponent } from './request-item/request-item';

// In routes
{
  path: 'request-item',
  component: RequestItemComponent,
  canActivate: [authGuard]
}
```

2. **Ensure HttpClientModule is provided:**

```typescript
// In your main.ts or app config
import { HttpClientModule } from '@angular/common/http';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(),
    // ... other providers
  ]
});
```

3. **Ensure environment variables are set:**

```typescript
// environments/environment.ts
export const environment = {
  production: false,
  apiUrl: '/api'
};
```

### Using the Services

```typescript
// In a component or service
import { ItemService } from './request-item/services/item.service';
import { RequestService } from './request-item/services/request.service';

constructor(
  private itemService: ItemService,
  private requestService: RequestService
) {}

// Get all items
this.itemService.getItems().subscribe(items => {
  console.log(items);
});

// Search items
this.itemService.searchItems('laptop', { inStockOnly: true }).subscribe(items => {
  console.log(items);
});

// Create request
const dto: CreateRequestDto = {
  items: [
    { itemId: 1, quantity: 2 },
    { itemId: 3, quantity: 1 }
  ]
};
this.requestService.createRequest(dto).subscribe(response => {
  console.log('Request ID:', response.id);
});

// Get request details
this.requestService.getRequestById(123).subscribe(request => {
  console.log(request);
});
```

## API Endpoints Expected

### Items
- `GET /api/inventory` - Get all items
- `GET /api/inventory/{id}` - Get single item
- `GET /api/inventory?categoryId={id}` - Get items by category

### Requests
- `POST /api/requests` - Create request
- `GET /api/requests/my` - Get user's requests
- `GET /api/requests/{id}` - Get request details
- `PATCH /api/requests/{id}/cancel` - Cancel request
- `DELETE /api/requests/{id}` - Delete request

## Customization Guide

### Change Cache Duration

```typescript
// In item.service.ts
private readonly CACHE_DURATION = 10 * 60 * 1000; // 10 minutes
```

### Add More Filters

```typescript
// 1. Extend ItemFilterOptions in request.model.ts
export interface ItemFilterOptions {
  searchText?: string;
  category?: string;
  inStockOnly?: boolean;
  minPrice?: number;      // Add this
  maxPrice?: number;      // Add this
}

// 2. Update filterItems() in item.service.ts
private filterItems(...): InventoryItem[] {
  // ... existing code
  if (filters.minPrice || filters.maxPrice) {
    result = result.filter(item => {
      if (filters.minPrice && item.price < filters.minPrice) return false;
      if (filters.maxPrice && item.price > filters.maxPrice) return false;
      return true;
    });
  }
  return result;
}

// 3. Add UI in template and component
```

### Replace Alert with Toast Notifications

```typescript
// 1. Inject ToastrService instead of using alert()
import { ToastrService } from 'ngx-toastr';

constructor(private toastr: ToastrService) {}

// 2. Replace in request-item.ts
private showSuccessMessage(message: string): void {
  this.toastr.success(message);
}

// 3. Replace in request-detail-modal.ts
// Use same approach
```

### Add Pagination

```typescript
// 1. Update ItemService.getItems()
getItems(page: number = 1, pageSize: number = 20): Observable<{ items: InventoryItem[], total: number }> {
  const params = new HttpParams()
    .set('page', page.toString())
    .set('pageSize', pageSize.toString());
  
  return this.http.get<{ items: InventoryItem[], total: number }>(
    this.API_URL, 
    { params }
  );
}

// 2. Update component template to add pagination controls
```

## Error Handling

### Service Error Handling

The services automatically handle and transform HTTP errors into user-friendly messages:

- **400 Bad Request**: "Invalid request data" or custom message
- **401 Unauthorized**: "Unauthorized. Please log in again."
- **403 Forbidden**: "Access denied"
- **404 Not Found**: "Request not found"
- **409 Conflict**: "Item already requested" or custom message
- **5xx Server Error**: "Server error. Please try again later."

### Component Error Display

Errors are displayed in alert boxes above the main content:

```html
<div class="alert alert-error" *ngIf="error$ | async as error">
  <span>{{ error }}</span>
</div>
```

Users can dismiss alerts with the close button or by calling `clearError()`.

## Performance Optimization

### 1. TrackBy Functions
```typescript
trackByItemId(index: number, item: InventoryItem | DraftItem): number {
  return item.id;
}

// Used in ngFor
<div *ngFor="let item of items; trackBy: trackByItemId">
```

### 2. Debounced Search
```typescript
this.searchForm.get('searchText')?.valueChanges.pipe(
  debounceTime(300),  // Wait 300ms before filtering
  takeUntil(this.destroy$)
).subscribe(() => this.applyFilters());
```

### 3. Item Caching
```typescript
// ItemService caches items for 5 minutes
// Use forceRefresh: true to bypass cache
this.itemService.getItems(true).subscribe(items => {});
```

### 4. OnDestroy Cleanup
```typescript
private destroy$ = new Subject<void>();

ngOnDestroy(): void {
  this.destroy$.next();
  this.destroy$.complete();
}

// All subscriptions use takeUntil(this.destroy$)
```

## Testing Recommendations

### Unit Tests

```typescript
// request.service.spec.ts
describe('RequestService', () => {
  it('should create request with items', () => {
    const dto: CreateRequestDto = {
      items: [{ itemId: 1, quantity: 2 }]
    };
    service.createRequest(dto).subscribe(response => {
      expect(response.id).toBeGreaterThan(0);
    });
  });
});

// item.service.spec.ts
describe('ItemService', () => {
  it('should cache items', () => {
    service.getItems().subscribe();
    service.getItems().subscribe(); // Should use cache
    expect(httpClient.get).toHaveBeenCalledTimes(1);
  });
});
```

### Integration Tests

```typescript
// request-item.component.spec.ts
describe('RequestItemComponent', () => {
  it('should load items on init', () => {
    component.ngOnInit();
    expect(component.items.length).toBeGreaterThan(0);
  });

  it('should add item to draft', () => {
    component.addToDraft(mockItem);
    expect(component.draftItems.length).toBe(1);
  });
});
```

## Styling Customization

### CSS Variables

The component uses CSS variables for theming. Update in your app.css:

```css
:root {
  --primary: #06b6d4;
  --danger: #ef4444;
  --bg-primary: #ffffff;
  --bg-secondary: #f8fafc;
  --bg-tertiary: #f1f5f9;
  --text-primary: #0f172a;
  --text-secondary: #64748b;
  --text-muted: #cbd5e1;
  --border: #e2e8f0;
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px rgba(0, 0, 0, 0.07);
  --shadow-lg: 0 10px 15px rgba(0, 0, 0, 0.1);
  --grad-main: linear-gradient(135deg, #06b6d4, #0891b2);
}
```

### Responsive Breakpoints

- **Desktop**: 1200px+ (2-column layout)
- **Tablet**: 768px - 1199px (single column)
- **Mobile**: Below 768px (full-width)

## Browser Support

- Chrome/Edge: Latest 2 versions
- Firefox: Latest 2 versions
- Safari: Latest 2 versions
- Mobile browsers: iOS Safari 14+, Chrome Mobile

## Future Enhancements

1. **Advanced Filtering**: Price range, rating, brand filters
2. **Sorting**: By name, price, availability, date added
3. **Bulk Operations**: Select multiple items, bulk add to draft
4. **Request History**: View past requests with analytics
5. **Notifications**: Real-time updates when request status changes
6. **Export**: Export request as PDF
7. **Integration**: Barcode scanning, voice search
8. **Personalization**: Save favorite items, recent searches

## Troubleshooting

### Items Not Loading
- Check network tab for API calls
- Verify API endpoint in environment.ts
- Check browser console for CORS errors
- Verify user has read permission on /api/inventory

### Request Submission Fails
- Verify draft has items (quantity > 0)
- Check for validation errors in HTTP 400 response
- Verify item quantities don't exceed available stock
- Check for HTTP 409 conflicts (item already requested)

### Modal Not Opening
- Ensure RequestDetailModalComponent is imported
- Verify `[isOpen]="showDetailModal"` binding
- Check browser console for component errors
- Verify `selectedRequestId` is set correctly

### Styles Not Applied
- Check CSS variable definitions in :root
- Verify BrowserAnimationsModule is provided
- Clear browser cache and rebuild
- Check for CSS file path in component decorator

## License

[Your License Here]

## Support

For issues or questions, please contact: [support@example.com]
