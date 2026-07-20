import { Component, DestroyRef, OnInit, DoCheck } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/services/service';
import { InventoryService } from '../services/inventory.service';
import { InventoryItem, Category, StockStatus } from '../models/item';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

/**
 * InventoryComponent
 * 
 * Displays and manages inventory items with:
 * - Table view of all items
 * - Add, Edit, Delete operations
 * - Stock increase/decrease buttons
 * - Duplicate item validation
 * - Real-time search and filtering
 * 
 * @production This component is production-ready with error handling and validation
 */
@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.html',
  styleUrls: ['./inventory.css']
})
export class InventoryComponent implements OnInit, DoCheck {

  // Data
  items: InventoryItem[] = [];
  filteredItems: InventoryItem[] = [];
  categories: Category[] = [];

  // Form state
  itemId: number | null = null;
  itemName = '';
  selectedCategoryId: number | null = null;
  quantity = 0;

  // UI state
  searchText = '';
  editingItemId: number | string | null = null;
  role: string = '';
  loading = false;
  errorMsg = '';
  successMsg = '';

  // For stock operations
  operatingItemId: number | string | null = null;
  operatingAction: 'increase' | 'decrease' | null = null;
  
  private searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  /**
   * Computed property: Items with stock < 10
   */
  get lowStockCount(): number {
    return this.items.filter(item => (item.availableQuantity || 0) < 10).length;
  }

  /**
   * Computed property: Total stock value
   */
  get totalInventoryValue(): number {
    return this.items.reduce((sum, item) => sum + ((item.availableQuantity || 0) * 1), 0);
  }

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private inventoryService: InventoryService,
    private destroyRef: DestroyRef
  ) {
    this.role = this.auth.getRole() ?? '';
  }

  ngOnInit(): void {
    // Load initial data
    this.loadInventory();
    this.loadCategories();

    // Subscribe to role changes
    this.auth.role$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(role => {
        this.role = role ?? '';
      });

    // Subscribe to inventory updates
    this.inventoryService.inventory$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(items => {
        this.items = items;
        this.applySearch();
      });

    // Subscribe to loading state
    this.inventoryService.loading$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(loading => {
        this.loading = loading;
      });

    // Subscribe to error messages
    this.inventoryService.error$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(error => {
        this.errorMsg = error;
        if (error) {
          setTimeout(() => this.errorMsg = '', 5000);
        }
      });

    // Setup debounced search
    this.searchSubject
      .pipe(debounceTime(300))
      .subscribe(() => this.applySearch());
  }

  /**
   * Auto filter on search text change
   */
  ngDoCheck(): void {
    // Note: Using ngDoCheck for simplicity, can be optimized with OnPush strategy
  }

  /**
   * ============================================
   * DATA LOADING
   * ============================================
   */

  /**
   * Load inventory items from service
   */
  loadInventory(): void {
    this.inventoryService.loadInventory()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err) => {
          console.error('Error loading inventory:', err);
        }
      });
  }

  /**
   * Load categories from service
   */
  loadCategories(): void {
    this.inventoryService.loadCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => {
          this.categories = categories;
        },
        error: (err) => {
          console.error('Error loading categories:', err);
        }
      });
  }

  /**
   * ============================================
   * CRUD OPERATIONS
   * ============================================
   */

  /**
   * Add new inventory item
   */
  addItem(): void {
    // Validation
    if (!this.validateForm()) return;

    // ✅ Item ID must be unique (manually entered, not auto-generated)
    const duplicateId = this.items.some(item => Number(item.id) === Number(this.itemId));
    if (duplicateId) {
      this.errorMsg = 'Item ID already exists. Please enter a unique Item ID.';
      setTimeout(() => this.errorMsg = '', 5000);
      return;
    }

    // ✅ Check for duplicates (case-insensitive)
    const normalizedName = this.normalizeItemName(this.itemName);
    const duplicateExists = this.items.some(item => 
      this.normalizeItemName(item.name) === normalizedName
    );
    
    if (duplicateExists) {
      this.errorMsg = `An item with the name "${this.itemName}" already exists. Please use a different name.`;
      setTimeout(() => this.errorMsg = '', 5000);
      return;
    }

    // Check role
    if (!this.hasInventoryPermission()) {
      this.errorMsg = 'Only Admins and Issuers can add inventory items';
      setTimeout(() => this.errorMsg = '', 5000);
      return;
    }

    const newItem = {
      name: this.itemName.trim(),
      categoryId: this.selectedCategoryId!,
      totalQuantity: this.quantity,
      description: 'New Item',
      category: this.getCategoryName(this.selectedCategoryId!),
      availableQuantity: this.quantity,
      id: Number(this.itemId),
      createdDate: new Date().toISOString()
    };

    this.inventoryService.addItem(newItem)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.successMsg = 'Item added successfully!';
          this.resetForm();
          setTimeout(() => this.successMsg = '', 3000);
        },
        error: (err) => {
          console.error('Error adding item:', err);
        }
      });
  }

  /**
   * Edit item - populate form with item data
   * @param item - Item to edit
   */
  editItem(item: InventoryItem): void {
    this.editingItemId = item.id;
    this.itemId = Number(item.id);
    this.itemName = item.name;
    this.selectedCategoryId = item.categoryId || null;
    this.quantity = item.availableQuantity || 0;
  }

  /**
   * Update existing item
   */
  updateItem(): void {
    // Validation
    if (!this.validateForm()) return;

    // ✅ Check for duplicate on name change (excluding self)
    const normalizedName = this.normalizeItemName(this.itemName);
    const duplicateExists = this.items.some(item => 
      item.id !== this.editingItemId && 
      this.normalizeItemName(item.name) === normalizedName
    );
    
    if (duplicateExists) {
      this.errorMsg = `An item with the name "${this.itemName}" already exists. Please use a different name.`;
      setTimeout(() => this.errorMsg = '', 5000);
      return;
    }

    // Check role
    if (!this.hasInventoryPermission()) {
      this.errorMsg = 'Only Admins and Issuers can update inventory items';
      setTimeout(() => this.errorMsg = '', 5000);
      return;
    }

    const updates = {
      name: this.itemName.trim(),
      categoryId: this.selectedCategoryId!,
      availableQuantity: this.quantity,
      description: 'Updated Item'
    };

    this.inventoryService.updateItem(this.editingItemId!, updates)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.successMsg = 'Item updated successfully!';
          this.resetForm();
          setTimeout(() => this.successMsg = '', 3000);
        },
        error: (err) => {
          console.error('Error updating item:', err);
        }
      });
  }

  /**
   * Delete inventory item
   * @param id - Item ID
   */
  deleteItem(id: number | string): void {
    // Check role
    if (!this.hasInventoryPermission()) {
      this.errorMsg = 'Only Admins and Issuers can delete inventory items';
      setTimeout(() => this.errorMsg = '', 5000);
      return;
    }

    if (confirm('Are you sure you want to delete this item?')) {
      this.inventoryService.deleteItem(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.successMsg = 'Item deleted successfully!';
            setTimeout(() => this.successMsg = '', 3000);
          },
          error: (err) => {
            console.error('Error deleting item:', err);
          }
        });
    }
  }

  /**
   * ============================================
   * STOCK OPERATIONS
   * ============================================
   */

  /**
   * Increase stock for item
   * @param item - Item to increase stock for
   */
  increaseStock(item: InventoryItem): void {
    if (!this.hasInventoryPermission()) {
      this.errorMsg = 'Only Admins and Issuers can modify stock';
      return;
    }

    this.operatingItemId = item.id;
    this.operatingAction = 'increase';

    this.inventoryService.increaseStock(item.id, 1)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.operatingItemId = null;
          this.operatingAction = null;
        },
        error: (err) => {
          this.operatingItemId = null;
          this.operatingAction = null;
          console.error('Error increasing stock:', err);
        }
      });
  }

  /**
   * Decrease stock for item
   * @param item - Item to decrease stock for
   */
  decreaseStock(item: InventoryItem): void {
    if (!this.hasInventoryPermission()) {
      this.errorMsg = 'Only Admins and Issuers can modify stock';
      return;
    }

    // Check if we can decrease
    if ((item.availableQuantity || 0) <= 0) {
      this.errorMsg = 'Cannot decrease stock below 0';
      setTimeout(() => this.errorMsg = '', 5000);
      return;
    }

    this.operatingItemId = item.id;
    this.operatingAction = 'decrease';

    this.inventoryService.decreaseStock(item.id, 1)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.operatingItemId = null;
          this.operatingAction = null;
        },
        error: (err) => {
          this.operatingItemId = null;
          this.operatingAction = null;
          console.error('Error decreasing stock:', err);
        }
      });
  }

  /**
   * ============================================
   * UTILITY METHODS
   * ============================================
   */

  /**
   * Get stock status badge class
   * @param quantity - Available quantity
   * @returns CSS class name
   */
  getStockStatusClass(quantity: number): string {
    if (quantity < 5) return StockStatus.CRITICAL;
    if (quantity < 20) return StockStatus.LOW_STOCK;
    return StockStatus.IN_STOCK;
  }

  /**
   * Get stock status label
   * @param quantity - Available quantity
   * @returns Status label
   */
  getStockStatusLabel(quantity: number): string {
    if (quantity < 5) return 'Critical';
    if (quantity < 20) return 'Low Stock';
    return 'In Stock';
  }

  /**
   * Get category name by ID
   * @param categoryId - Category ID
   * @returns Category name
   */
  getCategoryName(categoryId: number): string {
    const category = this.categories.find(c => c.id === categoryId);
    return category?.name || 'Unknown';
  }

  /**
   * Search items with debounce
   * @param value - Search term
   */
  onSearchChange(value: string): void {
    this.searchText = value;
    this.searchSubject.next(value);
  }

  /**
   * Apply search filter
   */
  private applySearch(): void {
    this.filteredItems = this.inventoryService.searchItems(this.searchText);
  }

  /**
   * Reset form to initial state
   */
  resetForm(): void {
    this.itemId = null;
    this.itemName = '';
    this.selectedCategoryId = null;
    this.quantity = 0;
    this.editingItemId = null;
  }

  /**
   * Cancel editing
   */
  cancelEdit(): void {
    this.resetForm();
  }

  /**
   * ============================================
   * VALIDATION & CHECKS
   * ============================================
   */

  /**
   * Validate form inputs
   * @returns true if valid, false otherwise
   */
  private validateForm(): boolean {
    if (!this.isEditing() && (this.itemId === null || Number(this.itemId) <= 0)) {
      this.errorMsg = 'Please enter a valid Item ID';
      setTimeout(() => this.errorMsg = '', 5000);
      return false;
    }

    if (!this.normalizeItemName(this.itemName)) {
      this.errorMsg = 'Please enter an item name';
      setTimeout(() => this.errorMsg = '', 5000);
      return false;
    }

    if (!this.selectedCategoryId) {
      this.errorMsg = 'Please select a category';
      setTimeout(() => this.errorMsg = '', 5000);
      return false;
    }

    if (this.quantity <= 0) {
      this.errorMsg = 'Quantity must be greater than 0';
      setTimeout(() => this.errorMsg = '', 5000);
      return false;
    }

    return true;
  }

  /**
   * Check if user has online stationary management system permission
   * @returns true if user is ADMIN or ISSUER
   */
  hasInventoryPermission(): boolean {
    if (!this.role) return false;
    const r = this.role.toUpperCase();
    return r === 'ADMIN' || r === 'ISSUER';
  }

  /**
   * Check if currently editing
   * @returns true if editing an item
   */
  isEditing(): boolean {
    return this.editingItemId !== null;
  }

  /**
   * Check if operation is in progress for item
   * @param itemId - Item ID
   * @param action - Action type
   * @returns true if operation is in progress
   */
  isOperating(itemId: number | string, action: 'increase' | 'decrease'): boolean {
    return this.operatingItemId === itemId && this.operatingAction === action;
  }

  /**
   * ✅ Check if item name already exists (case-insensitive)
   * Used for real-time validation feedback
   * @param itemName - Item name to check
   * @param excludeItemId - Item ID to exclude from check (for updates)
   * @returns true if duplicate exists
   */
  private normalizeItemName(value?: string): string {
    return (value ?? '').trim().toLowerCase();
  }

  isDuplicateItemName(itemName: string, excludeItemId?: number | string | null): boolean {
    const normalizedName = this.normalizeItemName(itemName);
    if (!normalizedName) return false;
    
    return this.items.some(item => {
      // Exclude the item being edited
      if (excludeItemId !== undefined && excludeItemId !== null && item.id === excludeItemId) {
        return false;
      }
      
      return this.normalizeItemName(item.name) === normalizedName;
    });
  }

  /**
   * ✅ Get duplicate item warning message
   * @returns Error message if duplicate exists, empty string otherwise
   */
  getDuplicateItemWarning(): string {
    if (!this.normalizeItemName(this.itemName)) return '';
    
    const isDuplicate = this.isDuplicateItemName(this.itemName, this.editingItemId);
    
    if (isDuplicate) {
      return `⚠️ An item named "${this.itemName}" already exists`;
    }
    
    return '';
  }

  /**
   * ✅ Check if submit button should be disabled due to duplicate
   * @returns true if submit should be disabled
   */
  isSubmitDisabledByDuplicate(): boolean {
    if (!this.normalizeItemName(this.itemName)) return false;
    
    return this.isDuplicateItemName(this.itemName, this.editingItemId);
  }

}
