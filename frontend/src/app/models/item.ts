export type ItemCategory = string;

export interface Item {
  id: string | number;
  name: string;
  category: ItemCategory;
  description?: string;
  categoryId?: number;
  totalQuantity?: number;
  availableQuantity?: number;
  createdDate?: string;
}

export interface InventoryItem extends Item {
  categoryId: number;
  totalQuantity: number;
  availableQuantity: number;
  createdDate: string;
}

export interface Category {
  id: number;
  name: string;
}

export interface InventoryActionResult {
  success: boolean;
  message: string;
  item?: InventoryItem;
}

export enum StockStatus {
  CRITICAL = 'critical',      // < 5
  LOW_STOCK = 'low-stock',    // 5-20
  IN_STOCK = 'in-stock'       // >= 20
}

