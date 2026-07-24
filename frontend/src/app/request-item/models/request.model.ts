/**
 * Request Item Module - Models & Types
 * Provides strongly typed interfaces for type safety across the application
 */

/**
 * Request status enumeration
 */
export enum RequestStatus {
  PendingWithIssuer = 'PendingWithIssuer',
  NotIssued = 'NotIssued',
  PendingAdminApproval = 'PendingAdminApproval',
  Requested = 'Requested',
  Pending = 'Pending',
  Issued = 'Issued',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Received = 'Received',
  Cancelled = 'Cancelled'
}

/**
 * Represents a single inventory item
 */
export interface InventoryItem {
  id: number;
  itemCode: string;
  name: string;
  category: string;
  categoryId: number;
  description?: string;
  availableQuantity: number;
  totalQuantity?: number;
  unit?: string;
  reorderLevel?: number;
}

/**
 * Represents an item in a request draft (with quantity)
 */
export interface DraftItem extends InventoryItem {
  quantity: number;
}

/**
 * Represents a single line item in a request
 */
export interface RequestItem {
  id: number;
  itemCode: string;
  itemName: string;
  description?: string;
  quantityRequested: number;
  quantityApproved: number;
  quantityIssued: number;
}

/**
 * Request summary (for list view)
 */
export interface RequestSummary {
  id: number;
  status: RequestStatus;
  createdAt: Date;
  updatedAt?: Date;
  itemCount?: number;
}

/**
 * Full request details (with items)
 */
export interface RequestDetail {
  id: number;
  userId: number;
  status: RequestStatus;
  createdAt: Date;
  updatedAt?: Date;
  items: RequestItem[];
}

/**
 * DTO for creating a new request from cart
 */
export interface CreateRequestDto {
  categoryId?: number;
  items: CreateRequestItemDto[];
}

/**
 * DTO for request line item during creation
 */
export interface CreateRequestItemDto {
  itemCode: string;
  quantity: number;
}

/**
 * API response wrapper
 */
export interface ApiResponse<T> {
  data?: T;
  message?: string;
  errors?: { [key: string]: string[] };
}

/**
 * Pagination parameters for list requests
 */
export interface PaginationParams {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

/**
 * Filter options for requests list
 */
export interface RequestFilterOptions {
  status?: RequestStatus;
  searchText?: string;
  dateFrom?: Date;
  dateTo?: Date;
}

/**
 * Filter options for inventory items
 */
export interface ItemFilterOptions {
  searchText?: string;
  category?: string;
  inStockOnly?: boolean;
}
