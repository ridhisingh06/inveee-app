export interface MonthlyRegisterRow {
  id: number;
  requestId: number;
  userId: number;
  userName: string;
  itemCode: string;
  itemName: string;
  status: string;
  requestDate: string;
  quantityRequested: number;
  quantityApproved: number;
  quantityIssued: number;
}

export interface MonthlyRegisterResult {
  year: number;
  month: number;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  totalQuantityRequested: number;
  totalQuantityApproved: number;
  totalQuantityIssued: number;
  data: MonthlyRegisterRow[];
}
