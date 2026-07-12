/**
 * Item Service
 * Handles all API communication for inventory item operations
 * Provides caching and efficient data management
 */

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError, finalize, shareReplay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { InventoryItem, ItemFilterOptions } from '../models/request.model';

@Injectable({
  providedIn: 'root'
})
export class ItemService {
  private readonly API_URL = `${environment.apiUrl}/inventory`;
  private loading$ = new BehaviorSubject<boolean>(false);
  private error$ = new BehaviorSubject<string | null>(null);
  
  // Cache for items (expires after 5 minutes)
  private itemsCache$?: Observable<InventoryItem[]>;
  private lastCacheTime: number = 0;
  private readonly CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

  constructor(private http: HttpClient) {}

  /**
   * Get loading state
   */
  getLoading$(): Observable<boolean> {
    return this.loading$.asObservable();
  }

  /**
   * Get error state
   */
  getError$(): Observable<string | null> {
    return this.error$.asObservable();
  }

  /**
   * Clear error
   */
  clearError(): void {
    this.error$.next(null);
  }

  /**
   * Get all inventory items with caching
   */
  getItems(forceRefresh: boolean = false): Observable<InventoryItem[]> {
    // Check cache validity
    if (!forceRefresh && this.itemsCache$ && this.isCacheValid()) {
      return this.itemsCache$;
    }

    this.setLoading(true);
    this.itemsCache$ = this.http.get<InventoryItem[]>(this.API_URL).pipe(
      tap(() => {
        this.lastCacheTime = Date.now();
        this.clearError();
      }),
      catchError(error => this.handleError(error)),
      finalize(() => this.setLoading(false)),
      shareReplay(1)
    );

    return this.itemsCache$;
  }

  /**
   * Search items by name or category
   */
  searchItems(searchText: string, filters?: ItemFilterOptions): Observable<InventoryItem[]> {
    return this.getItems().pipe(
      tap(items => {
        const results = this.filterItems(items, searchText, filters);
        return results;
      })
    );
  }

  /**
   * Get single item by ID
   */
  getItemById(id: number): Observable<InventoryItem> {
    return this.http.get<InventoryItem>(`${this.API_URL}/${id}`).pipe(
      catchError(error => this.handleError(error))
    );
  }

  /**
   * Get items by category
   */
  getItemsByCategory(categoryId: number): Observable<InventoryItem[]> {
    const params = new HttpParams().set('categoryId', categoryId.toString());
    return this.http.get<InventoryItem[]>(`${this.API_URL}`, { params }).pipe(
      catchError(error => this.handleError(error))
    );
  }

  /**
   * Get items in stock (availableQuantity > 0)
   */
  getItemsInStock(): Observable<InventoryItem[]> {
    return this.getItems().pipe(
      tap(items => {
        const inStock = items.filter(item => item.availableQuantity > 0);
        return inStock;
      })
    );
  }

  /**
   * Refresh cache explicitly
   */
  refreshCache(): Observable<InventoryItem[]> {
    return this.getItems(true);
  }

  /**
   * Invalidate cache
   */
  invalidateCache(): void {
    this.itemsCache$ = undefined;
    this.lastCacheTime = 0;
  }

  /**
   * Private helper: Check if cache is still valid
   */
  private isCacheValid(): boolean {
    return Date.now() - this.lastCacheTime < this.CACHE_DURATION;
  }

  /**
   * Private helper: Filter items based on criteria
   */
  private filterItems(
    items: InventoryItem[],
    searchText: string,
    filters?: ItemFilterOptions
  ): InventoryItem[] {
    let result = items;

    // Text search
    if (searchText) {
      const searchLower = searchText.toLowerCase();
      result = result.filter(item =>
        (item.name ?? '').toLowerCase().includes(searchLower) ||
        (item.category ?? '').toLowerCase().includes(searchLower)
      );
    }

    // Apply additional filters
    if (filters) {
      if (filters.category) {
        result = result.filter(item => item.category === filters.category);
      }
      if (filters.inStockOnly) {
        result = result.filter(item => item.availableQuantity > 0);
      }
    }

    return result;
  }

  /**
   * Private helper: Set loading state
   */
  private setLoading(isLoading: boolean): void {
    this.loading$.next(isLoading);
  }

  /**
   * Private helper: Handle HTTP errors
   */
  private handleError(error: any): Observable<never> {
    let message = 'Failed to load items';

    if (error.error instanceof ErrorEvent) {
      message = `Error: ${error.error.message}`;
    } else if (error.status === 404) {
      message = 'Items not found';
    } else if (error.status === 401) {
      message = 'Unauthorized. Please log in again.';
    } else if (error.status >= 500) {
      message = 'Server error. Please try again later.';
    }

    this.error$.next(message);
    console.error('Item Service Error:', message, error);
    return throwError(() => new Error(message));
  }
}
