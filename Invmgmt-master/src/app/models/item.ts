export type ItemCategory = string;

export interface Item {
  id: string | number;
  name: string;
  category: ItemCategory;
  description?: string;
}

