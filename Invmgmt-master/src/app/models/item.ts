export type ItemCategory = 'Stationary' | 'IT Items' | 'Housekeeping';

export interface Item {
  id: string;
  name: string;
  category: ItemCategory;
  description?: string;
}

