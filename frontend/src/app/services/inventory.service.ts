import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { InventoryItem, Category, InventoryActionResult } from '../models/item';
import { RefreshService } from './refresh.service';

/**
 * InventoryService
 * 
 * Manages all inventory-related operations including:
 * - CRUD operations for inventory items
 * - Stock management (increase/decrease)
 * - Duplicate detection and validation
 * - Inventory state management
 * 
 * @production This service is production-ready with error handling and caching
 */
@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  
  private apiUrl = `${environment.apiUrl}/inventory`;
  private categoriesUrl = `${environment.apiUrl}/ItemCategory`;
  
  // State management with RxJS
  private inventorySubject = new BehaviorSubject<InventoryItem[]>([]);
  public inventory$ = this.inventorySubject.asObservable();
  
  private categoriesSubject = new BehaviorSubject<Category[]>([]);
  public categories$ = this.categoriesSubject.asObservable();
  
  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();
  
  private errorSubject = new BehaviorSubject<string>('');
  public error$ = this.errorSubject.asObservable();
  
  // Cache for quick lookup
  private itemsCache: Map<number | string, InventoryItem> = new Map();
  
  constructor(private http: HttpClient, private refresh: RefreshService) {}
  
  /**
   * Load all inventory items from API
   * @returns Observable<InventoryItem[]>
   */
  loadInventory(): Observable<InventoryItem[]> {
    this.loadingSubject.next(true);
    this.errorSubject.next('');
    
    return this.http.get<InventoryItem[]>(this.apiUrl)
      .pipe(
        tap(items => {
          this.inventorySubject.next(items);
          this.updateCache(items);
          this.loadingSubject.next(false);
        }),
        catchError(err => {
          const errorMsg = this.handleError(err, 'Failed to load inventory');
          this.errorSubject.next(errorMsg);
          this.loadingSubject.next(false);
          return throwError(() => err);
        })
      );
  }
  
  /**
   * Load all categories from API
   * @returns Observable<Category[]>
   */
  loadCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(this.categoriesUrl)
      .pipe(
        tap(categories => {
          this.categoriesSubject.next(categories);
        }),
        catchError(err => {
          this.handleError(err, 'Failed to load categories');
          return throwError(() => err);
        })
      );
  }
  
  /**
   * Add a new inventory item with duplicate detection
   * 
   * @param item - Item to add (name, categoryId, totalQuantity)
   * @returns Observable<InventoryItem>
   * @throws Error if duplicate item exists
   */
  addItem(item: Omit<InventoryItem, 'id' | 'availableQuantity' | 'createdDate'>): Observable<InventoryItem> {
    // Validate for duplicates BEFORE API call
    const duplicateError = this.validateNoDuplicate(item.name, null);
    if (duplicateError) {
      this.errorSubject.next(duplicateError);
      return throwError(() => new Error(duplicateError));
    }
    
    const payload = {
      ...item,
      description: item.description || 'New Item'
    };
    
    this.loadingSubject.next(true);
    
    return this.http.post<InventoryItem>(this.apiUrl, payload)
      .pipe(
        tap(newItem => {
          const current = this.inventorySubject.value;
          this.inventorySubject.next([...current, newItem]);
          this.itemsCache.set(newItem.id, newItem);
          this.errorSubject.next('');
          this.loadingSubject.next(false);
          // ✅ Notify any subscribers (e.g. UserItemListComponent) that
          //    inventory has changed so they can refresh without a page reload.
          this.refresh.notifyInventory();
        }),
        catchError(err => {
          let errorMsg = this.handleError(err, 'Failed to add item');
          
          //  Handle backend duplicate check error
          if (err?.status === 400 && err?.error?.message && err.error.message.includes('already exists')) {
            errorMsg = err.error.message;
          }
          
          this.errorSubject.next(errorMsg);
          this.loadingSubject.next(false);
          return throwError(() => err);
        })
      );
  }
  
  /**
   * Update an existing inventory item
   * 
   * @param id - Item ID
   * @param item - Updated item data
   * @returns Observable<InventoryItem>
   */
  updateItem(id: number | string, item: Partial<InventoryItem>): Observable<InventoryItem> {
    // Check for duplicate on name change (excluding self)
    if (item.name) {
      const duplicateError = this.validateNoDuplicate(item.name, id);
      if (duplicateError) {
        this.errorSubject.next(duplicateError);
        return throwError(() => new Error(duplicateError));
      }
    }
    
    this.loadingSubject.next(true);
    
    return this.http.put<InventoryItem>(`${this.apiUrl}/${id}`, item)
      .pipe(
        tap(updatedItem => {
          const current = this.inventorySubject.value;
          const index = current.findIndex(i => i.id === id);
          if (index !== -1) {
            current[index] = updatedItem;
            this.inventorySubject.next([...current]);
          }
          this.itemsCache.set(id, updatedItem);
          this.errorSubject.next('');
          this.loadingSubject.next(false);
          // ✅ Notify subscribers that an item was updated.
          this.refresh.notifyInventory();
        }),
        catchError(err => {
          const errorMsg = this.handleError(err, 'Failed to update item');
          this.errorSubject.next(errorMsg);
          this.loadingSubject.next(false);
          return throwError(() => err);
        })
      );
  }
  
  /**
   * Delete an inventory item
   * 
   * @param id - Item ID
   * @returns Observable<any>
   */
  deleteItem(id: number | string): Observable<any> {
    this.loadingSubject.next(true);
    
    return this.http.delete(`${this.apiUrl}/${id}`)
      .pipe(
        tap(() => {
          const current = this.inventorySubject.value;
          this.inventorySubject.next(current.filter(i => i.id !== id));
          this.itemsCache.delete(id);
          this.errorSubject.next('');
          this.loadingSubject.next(false);
          // ✅ Notify subscribers that an item was deleted.
          this.refresh.notifyInventory();
        }),
        catchError(err => {
          const errorMsg = this.handleError(err, 'Failed to delete item');
          this.errorSubject.next(errorMsg);
          this.loadingSubject.next(false);
          return throwError(() => err);
        })
      );
  }
  
  /**
   * Increase stock for an item
   * 
   * @param id - Item ID
   * @param quantity - Amount to increase
   * @returns Observable<InventoryItem>
   */
  increaseStock(id: number | string, quantity: number): Observable<InventoryItem> {
    if (quantity <= 0) {
      const error = 'Stock increase quantity must be positive';
      this.errorSubject.next(error);
      return throwError(() => new Error(error));
    }

    this.loadingSubject.next(true);

    return this.http.patch<any>(`${this.apiUrl}/${id}/increase-stock`, { quantity })
      .pipe(
        tap((response) => {
          const updatedItem: InventoryItem = {
            id: response.id,
            name: response.name,
            categoryId: response.categoryId,
            category: response.category,
            availableQuantity: response.availableQuantity,
            totalQuantity: response.totalQuantity,
            description: response.description,
            createdDate: new Date().toISOString()
          };
          
          const current = this.inventorySubject.value;
          const index = current.findIndex(i => i.id === id);
          if (index !== -1) {
            current[index] = updatedItem;
            this.inventorySubject.next([...current]);
          }
          this.itemsCache.set(id, updatedItem);
          this.errorSubject.next('');
          this.loadingSubject.next(false);
          // ✅ Notify subscribers after stock increase.
          this.refresh.notifyInventory();
        }),
        catchError(err => {
          const errorMsg = this.handleError(err, 'Failed to increase stock');
          this.errorSubject.next(errorMsg);
          this.loadingSubject.next(false);
          return throwError(() => err);
        })
      );
  }
  
  decreaseStock(id: number | string, quantity: number): Observable<InventoryItem> {
    if (quantity <= 0) {
      const error = 'Stock decrease quantity must be positive';
      this.errorSubject.next(error);
      return throwError(() => new Error(error));
    }

    const currentItem = this.itemsCache.get(id);
    if (!currentItem) {
      const error = 'Item not found';
      this.errorSubject.next(error);
      return throwError(() => new Error(error));
    }

    const available = currentItem.availableQuantity || 0;
    if (available < quantity) {
      const error = `Insufficient stock. Available: ${available}, Requested: ${quantity}`;
      this.errorSubject.next(error);
      return throwError(() => new Error(error));
    }

    this.loadingSubject.next(true);

    return this.http.patch<any>(`${this.apiUrl}/${id}/decrease-stock`, { quantity })
      .pipe(
        tap((response) => {
          const updatedItem: InventoryItem = {
            id: response.id,
            name: response.name,
            categoryId: response.categoryId,
            category: response.category,
            availableQuantity: response.availableQuantity,
            totalQuantity: response.totalQuantity,
            description: response.description,
            createdDate: new Date().toISOString()
          };
          
          const current = this.inventorySubject.value;
          const index = current.findIndex(i => i.id === id);
          if (index !== -1) {
            current[index] = updatedItem;
            this.inventorySubject.next([...current]);
          }
          this.itemsCache.set(id, updatedItem);
          this.errorSubject.next('');
          this.loadingSubject.next(false);
          // ✅ Notify subscribers after stock decrease.
          this.refresh.notifyInventory();
        }),
        catchError(err => {
          const errorMsg = this.handleError(err, 'Failed to decrease stock');
          this.errorSubject.next(errorMsg);
          this.loadingSubject.next(false);
          return throwError(() => err);
        })
      );
  }
  
  /**
   * Get current inventory items from state
   * @returns InventoryItem[]
   */
  getInventorySnapshot(): InventoryItem[] {
    return this.inventorySubject.value;
  }
  
  /**
   * Get current categories from state
   * @returns Category[]
   */
  getCategoriesSnapshot(): Category[] {
    return this.categoriesSubject.value;
  }
  
  /**
   * Clear error message
   */
  clearError(): void {
    this.errorSubject.next('');
  }
  
  /**
   * ============================================
   * VALIDATION & UTILITY METHODS
   * ============================================
   */
  
  /**
   * Validate that no duplicate item exists by name or ID
   * Returns error message if duplicate found, empty string if valid
   * 
   * @param itemName - Item name to check
   * @param excludeId - ID to exclude from check (for updates)
   * @returns Error message or empty string
   */
  private normalizeText(value?: string): string {
    return (value ?? '').trim().toLowerCase();
  }

  private validateNoDuplicate(itemName: string, excludeId: number | string | null): string {
    const trimmedName = this.normalizeText(itemName);
    
    const currentItems = this.inventorySubject.value;
    
    const isDuplicate = currentItems.some(item => {
      // If this is an update, exclude the current item
      if (excludeId !== null && item.id === excludeId) {
        return false;
      }
      
      // Check if name matches (case-insensitive)
      return this.normalizeText(item.name) === trimmedName;
    });
    
    if (isDuplicate) {
      return `An item with the name "${itemName}" already exists. Please use a different name.`;
    }
    
    return '';
  }
  
  /**
   * Update the items cache for quick lookups
   * @param items - Items to cache
   */
  private updateCache(items: InventoryItem[]): void {
    this.itemsCache.clear();
    items.forEach(item => {
      this.itemsCache.set(item.id, item);
    });
  }
  
  /**
   * Handle HTTP errors with user-friendly messages
   * @param error - Error object
   * @param defaultMessage - Default message if specific error handling fails
   * @returns User-friendly error message
   */
  private handleError(error: any, defaultMessage: string): string {
    if (error?.error?.message) {
      return error.error.message;
    }
    
    if (error?.status === 401) {
      return 'Unauthorized. Please check your permissions.';
    }
    
    if (error?.status === 403) {
      return 'Forbidden. You do not have permission to perform this action.';
    }
    
    if (error?.status === 404) {
      return 'Item not found.';
    }
    
    if (error?.status === 409) {
      return 'Conflict. This item may have been modified by another user.';
    }
    
    if (error?.status >= 500) {
      return 'Server error. Please try again later.';
    }
    
    return defaultMessage;
  }
  
  /**
   * Check if an item name already exists
   * @param itemName - Name to check
   * @returns boolean
   */
  itemNameExists(itemName: string): boolean {
    const trimmedName = this.normalizeText(itemName);
    return this.inventorySubject.value.some(
      item => this.normalizeText(item.name) === trimmedName
    );
  }
  
  /**
   * Search inventory items
   * @param searchTerm - Search term
   * @returns Filtered InventoryItem[]
   */
  searchItems(searchTerm: string): InventoryItem[] {
    const term = this.normalizeText(searchTerm);
    if (!term) return this.inventorySubject.value;
    
    return this.inventorySubject.value.filter(item =>
      this.normalizeText(item.name).includes(term) ||
      item.id.toString().includes(term)
    );
  }
}
