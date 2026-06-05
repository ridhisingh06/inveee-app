/**
 * Request Item Component
 * Production-ready component for browsing items and creating requests
 * Features: Search, filter, add to draft, submit for approval
 */

import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { Subject, Observable } from 'rxjs';
import { takeUntil, debounceTime } from 'rxjs/operators';

import { ItemService } from './services/item.service';
import { RequestService } from './services/request.service';
import {
  InventoryItem,
  DraftItem,
  CreateRequestDto,
  ItemFilterOptions
} from './models/request.model';
import { RequestDetailModalComponent } from './components/request-detail-modal/request-detail-modal.component';

@Component({
  selector: 'app-request-item',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RequestDetailModalComponent
  ],
  templateUrl: './request-item.html',
  styleUrls: ['./request-item.css']
})
export class RequestItemComponent implements OnInit, OnDestroy {
  // Data
  items: InventoryItem[] = [];
  filteredItems: InventoryItem[] = [];
  draftItems: DraftItem[] = [];

  // State management - Initialized in ngOnInit
  loading$!: Observable<boolean>;
  requestLoading$!: Observable<boolean>;
  error$!: Observable<string | null>;
  requestError$!: Observable<string | null>;

  // UI state
  selectedCategory: string = '';
  showDetailModal = false;
  selectedRequestId: number | null = null;
  showStockOnly = false;
  localErrorMsg = '';
  successMsg = '';

  // Search form
  searchForm: FormGroup;

  // Lifecycle management
  private destroy$ = new Subject<void>();

  constructor(
    public itemService: ItemService,
    public requestService: RequestService,
    private fb: FormBuilder
  ) {
    this.searchForm = this.fb.group({
      searchText: [''],
      category: ['']
    });
  }

  ngOnInit(): void {
    // Initialize Observable properties after services are available
    this.loading$ = this.itemService.getLoading$();
    this.requestLoading$ = this.requestService.getLoading$();
    this.error$ = this.itemService.getError$();
    this.requestError$ = this.requestService.getError$();

    this.loadItems();
    this.setupSearchListener();
    this.setupCategoryListener();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all inventory items with proper error handling
   */
  private loadItems(): void {
    this.itemService
      .getItems()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items: InventoryItem[]) => {
          this.items = items;
          this.localErrorMsg = '';
          this.applyFilters();
        },
        error: (error) => {
          console.error('[RequestItemComponent] Error loading items:', error);
          this.localErrorMsg = error?.message || 'Failed to load items. Please try again.';
        }
      });
  }

  /**
   * Setup real-time search listener with debounce
   */
  private setupSearchListener(): void {
    this.searchForm
      .get('searchText')
      ?.valueChanges.pipe(
        debounceTime(300),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.applyFilters();
      });
  }

  /**
   * Setup category filter listener
   */
  private setupCategoryListener(): void {
    this.searchForm
      .get('category')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.applyFilters();
      });
  }

  /**
   * Apply all active filters to items
   */
  private applyFilters(): void {
    let result = [...this.items];
    const searchText = this.searchForm.get('searchText')?.value || '';
    const category = this.searchForm.get('category')?.value || '';

    // Search filter
    if (searchText) {
      const searchLower = searchText.toLowerCase();
      result = result.filter(
        item =>
          item.name.toLowerCase().includes(searchLower) ||
          item.category.toLowerCase().includes(searchLower)
      );
    }

    // Category filter
    if (category) {
      result = result.filter(item => item.category === category);
    }

    // Stock filter
    if (this.showStockOnly) {
      result = result.filter(item => item.availableQuantity > 0);
    }

    this.filteredItems = result;
  }

  /**
   * Get unique categories for filter dropdown
   */
  getCategories(): string[] {
    return [...new Set(this.items.map(item => item.category))].sort();
  }

  /**
   * Add item to draft request
   */
  addToDraft(item: InventoryItem): void {
    const existingItem = this.draftItems.find(d => d.id === item.id);

    if (existingItem) {
      existingItem.quantity++;
    } else {
      this.draftItems.push({
        ...item,
        quantity: 1
      });
    }
  }

  /**
   * Remove item from draft
   */
  removeFromDraft(itemId: number): void {
    this.draftItems = this.draftItems.filter(item => item.id !== itemId);
  }

  /**
   * Update quantity in draft
   */
  updateDraftQuantity(itemId: number, quantity: number): void {
    if (quantity <= 0) {
      this.removeFromDraft(itemId);
      return;
    }

    const item = this.draftItems.find(d => d.id === itemId);
    if (item) {
      item.quantity = Math.min(quantity, item.availableQuantity);
    }
  }

  /**
   * Get total items in draft
   */
  getDraftTotal(): number {
    return this.draftItems.reduce((sum, item) => sum + item.quantity, 0);
  }

  /**
   * Submit draft as request for approval with proper error handling
   */
  submitRequest(): void {
    if (this.draftItems.length === 0) {
      this.localErrorMsg = 'Please add items to the draft before submitting.';
      return;
    }

    const createRequestDto: CreateRequestDto = {
      items: this.draftItems.map(item => ({
        itemId: item.id,
        quantity: item.quantity
      }))
    };

    this.localErrorMsg = '';
    this.successMsg = '';

    this.requestService
      .createRequest(createRequestDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.successMsg = 'Request submitted successfully!';
          this.draftItems = [];
          this.selectedRequestId = response.id;
          this.showDetailModal = true;
          
          // Auto-hide success message after 3 seconds
          setTimeout(() => { this.successMsg = ''; }, 3000);
        },
        error: (error) => {
          console.error('[RequestItemComponent] Error submitting request:', error);
          this.localErrorMsg = error?.message || 'Failed to submit request. Please try again.';
        }
      });
  }

  /**
   * Clear draft
   */
  clearDraft(): void {
    if (confirm('Are you sure you want to clear the draft?')) {
      this.draftItems = [];
      this.localErrorMsg = '';
    }
  }

  /**
   * Toggle stock filter
   */
  toggleStockFilter(): void {
    this.showStockOnly = !this.showStockOnly;
    this.applyFilters();
  }

  /**
   * Refresh items list with proper error handling
   */
  refreshItems(): void {
    this.localErrorMsg = '';
    
    this.itemService.refreshCache()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.items = items;
          this.applyFilters();
        },
        error: (error) => {
          console.error('[RequestItemComponent] Error refreshing items:', error);
          this.localErrorMsg = error?.message || 'Failed to refresh items. Please try again.';
        }
      });
  }

  /**
   * Close detail modal
   */
  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedRequestId = null;
  }

  /**
   * Track by function for ngFor optimization
   */
  trackByItemId(index: number, item: InventoryItem | DraftItem): number {
    return item.id;
  }
}
