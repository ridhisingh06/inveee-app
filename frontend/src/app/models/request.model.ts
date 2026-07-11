// ============================================================
// BASIC REQUEST MODELS (backward-compatible with existing code)
// ============================================================

/** Mirrors backend RequestItemDetailDto */
export interface RequestItemDetail {
  id: number;
  itemId: number;
  itemName: string;
  quantityRequested: number;
  quantityApproved: number;
  quantityIssued: number;
  status?: string;
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

// ============================================================
// ISSUER PARTIAL-ISSUING DTOs
// ============================================================

/** One pending item shown to the issuer (from GET /api/issuer/pending) */
export interface IssuerPendingItem {
  requestItemId: number;
  itemId: number;
  itemName: string;
  requestedQuantity: number;
  availableQuantity: number;
  categoryName: string;
  status: string;
  requestId: number;
  requestedByUserName: string;
  requestedDate: string;
}

/** Paginated list returned by GET /api/issuer/pending */
export interface IssuerPendingList {
  items: IssuerPendingItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/** One item line in a partial-issue submission */
export interface IssueItemPartially {
  requestItemId: number;
  issueQuantity: number;
  rejectQuantity: number;
}

/** Body for PUT /api/issuer/issue-partially */
export interface IssuePartiallyPayload {
  requestId: number;
  items: IssueItemPartially[];
}

/** Detail of an issued item (inside IssuePartiallyResponse) */
export interface IssuedItemDetail {
  requestItemId: number;
  itemId: number;
  itemName: string;
  requestedQuantity: number;
  issuedQuantity: number;
  rejectedQuantity: number;
}

/** Response from PUT /api/issuer/issue-partially */
export interface IssuePartiallyResponse {
  success: boolean;
  message: string;
  requestId: number;
  issuedDate: string;
  issuedItems: IssuedItemDetail[];
}

// ============================================================
// ADMIN PARTIAL-APPROVAL DTOs
// ============================================================

/** One pending item shown to the admin (from GET /api/admin/pending) */
export interface AdminPendingItem {
  requestItemId: number;
  itemId: number;
  itemName: string;
  requestedQuantity: number;
  issuerIssuedQuantity: number;
  issuerRejectedQuantity: number;
  status: string;
  requestId: number;
  requestedByUserName: string;
  issuedByUserName: string;
  issuedDate: string;
}

/** Paginated list returned by GET /api/admin/pending */
export interface AdminPendingList {
  items: AdminPendingItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/** One item line in a partial-approval submission */
export interface ApproveItemPartially {
  requestItemId: number;
  approveQuantity: number;
  rejectQuantity: number;
}

/** Body for PUT /api/admin/approve-partially */
export interface ApprovePartiallyPayload {
  requestId: number;
  items: ApproveItemPartially[];
}

/** Detail of an approved item */
export interface ApprovedItemDetail {
  requestItemId: number;
  itemId: number;
  itemName: string;
  issuerIssuedQuantity: number;
  approvedQuantity: number;
  rejectedQuantity: number;
}

/** Response from PUT /api/admin/approve-partially */
export interface ApprovePartiallyResponse {
  success: boolean;
  message: string;
  requestId: number;
  approvedDate: string;
  approvedItems: ApprovedItemDetail[];
}

// ============================================================
// USER RECEIVE DTOs
// ============================================================

/** Response from POST /api/request/receive-items/{id} */
export interface ReceiveItemsResponse {
  success: boolean;
  message: string;
  requestId: number;
  orderSummaryId: number;
  receivedDate: string;
}

// ============================================================
// ORDER SUMMARY DTOs (receipt)
// ============================================================

/** One line item in the order summary */
export interface OrderSummaryItem {
  itemId: number;
  itemName: string;
  categoryName: string;
  requestedQuantity: number;
  issuedQuantity: number;
  issuerRejectedQuantity: number;
  approvedQuantity: number;
  adminRejectedQuantity: number;
  receivedQuantity: number;
}

/** Full order summary (receipt) from GET /api/request/orders/{id} */
export interface OrderSummary {
  id: number;
  requestId: number;
  userId: number;
  userName: string;
  userEmail: string;
  issuedByUserId?: number;
  issuedByUserName?: string;
  approvedByUserId?: number;
  approvedByUserName?: string;
  requestedDate: string;
  issuedDate: string;
  approvedDate: string;
  receivedDate: string;
  totalRequestedQuantity: number;
  totalIssuedQuantity: number;
  totalApprovedQuantity: number;
  totalRejectedQuantity: number;
  totalReceivedQuantity: number;
  status: string;
  items: OrderSummaryItem[];
  notes?: string;
  createdAt: string;
}

// ============================================================
// ORDER HISTORY DTOs
// ============================================================

/** One row in the order history list */
export interface OrderHistoryItem {
  id: number;
  requestId: number;
  receivedDate: string;
  status: string;
  totalRequestedQuantity: number;
  totalApprovedQuantity: number;
  totalRejectedQuantity: number;
  totalReceivedQuantity: number;
  itemCount: number;
}

/** Paginated order history from GET /api/request/orders */
export interface OrderHistoryList {
  orders: OrderHistoryItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/** Order stats from GET /api/request/order-stats */
export interface OrderStatistics {
  totalOrders: number;
  totalItemsReceived: number;
  totalItemsRequested: number;
  totalItemsRejected: number;
  completedOrders: number;
  pendingOrders: number;
}

// ============================================================
// ENHANCED REQUEST DETAIL (with partial qty breakdown)
// ============================================================

/** Enhanced request item detail showing all quantities */
export interface RequestItemDetailEnhanced {
  id: number;
  itemId: number;
  itemName: string;
  categoryName: string;
  quantityRequested: number;
  issuerIssuedQuantity: number;
  issuerRejectedQuantity: number;
  issuedDate?: string;
  issuedByUserName?: string;
  adminApprovedQuantity: number;
  adminRejectedQuantity: number;
  approvedDate?: string;
  approvedByUserName?: string;
  receivedQuantity: number;
  receivedDate?: string;
  status: string;
}

/** Enhanced request detail with full workflow breakdown */
export interface RequestDetailEnhanced {
  id: number;
  userId: number;
  status: string;
  createdAt: string;
  updatedAt?: string;
  issuedDate?: string;
  issuedByUserId?: number;
  issuedByUserName?: string;
  approvedDate?: string;
  approvedByUserId?: number;
  approvedByUserName?: string;
  receivedDate?: string;
  items: RequestItemDetailEnhanced[];
}
