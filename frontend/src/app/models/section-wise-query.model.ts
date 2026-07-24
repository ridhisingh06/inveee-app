export interface SectionWiseQueryOfficer {
  id: number;
  name: string;
  building?: string;
}

export interface SectionWiseQueryItem {
  id: number;
  itemCode: string;
  name: string;
  category?: string;
  availableQuantity: number;
}

export interface SectionWiseQueryFilter {
  officerId?: number;
  fromDate?: string;
  toDate?: string;
  bhawan?: string;
  itemCode?: string;
  itemName?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface SectionWiseQueryRow {
  requestItemId: number;
  requestId: number;
  officerName: string;
  bhawan?: string;
  itemCode: string;
  itemName: string;
  quantityRequested: number;
  quantityApproved: number;
  quantityIssued: number;
  requestStatus: string;
  requestDate: string;
  requestedBy: string;
}

export interface SectionWiseQueryResult {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  data: SectionWiseQueryRow[];
}
