/** Mirrors backend RequestItemDetailDto */
export interface RequestItemDetail {
  id: number;
  itemId: number;
  itemName: string;
  quantityRequested: number;
  quantityApproved: number;
  quantityIssued: number;
}

/** Mirrors backend RequestSummaryDto */
export interface RequestSummary {
  id: number;
  status: string;
  createdAt: string;
  updatedAt?: string;
}

/** Mirrors backend RequestDetailDto */
export interface RequestDetail {
  id: number;
  userId: number;
  status: string;
  createdAt: string;
  updatedAt?: string;
  items: RequestItemDetail[];
}

/** Payload to create a request from the cart */
export interface CreateRequestPayload {
  categoryId: number | null;
  items: { itemId: number; quantity: number }[];
}
