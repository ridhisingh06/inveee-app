import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  IssuerPendingList,
  IssuePartiallyPayload,
  IssuePartiallyResponse,
  AdminPendingList,
  ApprovePartiallyPayload,
  ApprovePartiallyResponse,
  ReceiveItemsResponse,
  OrderHistoryList,
  OrderSummary,
  OrderStatistics,
} from '../models/request.model';

/**
 * WorkflowService
 *
 * Covers the enterprise workflow endpoints that the existing RequestService
 * does not handle:
 *  - Issuer partial issuing (GET /api/issuer/pending, PUT /api/issuer/issue-partially)
 *  - Admin partial approval (GET /api/admin/pending, PUT /api/admin/approve-partially)
 *  - User receive items (POST /api/request/receive-items/{id})
 *  - Order history (GET /api/request/orders)
 *  - Order summary receipt (GET /api/request/orders/{id})
 */
@Injectable({ providedIn: 'root' })
export class WorkflowService {
  private readonly issuerBase  = `${environment.apiUrl}/issuer`;
  private readonly adminBase   = `${environment.apiUrl}/admin`;
  private readonly requestBase = `${environment.apiUrl}/requests`;

  constructor(private http: HttpClient) {}

  // ──────────────────────────────────────────────────────────
  // ISSUER
  // ──────────────────────────────────────────────────────────

  /**
   * GET /api/issuer/pending
   * Returns items grouped by request that are waiting for the issuer.
   * Each item includes availableQuantity (real-time from inventory).
   */
  getIssuerPendingItems(pageNumber = 1, pageSize = 20): Observable<IssuerPendingList> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http
      .get<IssuerPendingList>(`${this.issuerBase}/pending`, { params })
      .pipe(catchError(this.handleError));
  }

  /**
   * PUT /api/issuer/issue-partially
   * Issuer submits partial quantities for a request.
   * IssueQuantity + RejectQuantity must equal RequestedQuantity for every item.
   * Inventory is deducted atomically inside a DB transaction.
   */
  issuePartially(payload: IssuePartiallyPayload): Observable<IssuePartiallyResponse> {
    return this.http
      .put<IssuePartiallyResponse>(`${this.issuerBase}/issue-partially`, payload)
      .pipe(catchError(this.handleError));
  }

  // ──────────────────────────────────────────────────────────
  // ADMIN
  // ──────────────────────────────────────────────────────────

  /**
   * GET /api/admin/pending
   * Returns items waiting for admin approval that were issued by the issuer.
   */
  getAdminPendingItems(pageNumber = 1, pageSize = 20): Observable<AdminPendingList> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http
      .get<AdminPendingList>(`${this.adminBase}/pending`, { params })
      .pipe(catchError(this.handleError));
  }

  /**
   * PUT /api/admin/approve-partially
   * Admin submits partial approval. ApproveQuantity + RejectQuantity = IssuerIssuedQuantity.
   * Rejected quantities are restored to inventory inside a DB transaction.
   */
  approvePartially(payload: ApprovePartiallyPayload): Observable<ApprovePartiallyResponse> {
    return this.http
      .put<ApprovePartiallyResponse>(`${this.adminBase}/approve-partially`, payload)
      .pipe(catchError(this.handleError));
  }

  // ──────────────────────────────────────────────────────────
  // USER RECEIVE
  // ──────────────────────────────────────────────────────────

  /**
   * POST /api/request/receive-items/{id}
   * User confirms receipt of approved items.
   * Creates an immutable OrderSummary record.
   */
  receiveItems(requestId: number, notes?: string): Observable<ReceiveItemsResponse> {
    return this.http
      .post<ReceiveItemsResponse>(
        `${this.requestBase}/receive-items/${requestId}`,
        notes ? { notes } : {}
      )
      .pipe(catchError(this.handleError));
  }

  // ──────────────────────────────────────────────────────────
  // ORDER HISTORY
  // ──────────────────────────────────────────────────────────

  /**
   * GET /api/request/orders
   * Returns paginated order history for the current user.
   */
  getOrderHistory(pageNumber = 1, pageSize = 10): Observable<OrderHistoryList> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http
      .get<OrderHistoryList>(`${this.requestBase}/orders`, { params })
      .pipe(catchError(this.handleError));
  }

  /**
   * GET /api/request/orders/{id}
   * Returns the complete order summary (receipt) for a given order summary ID.
   */
  getOrderSummaryById(orderSummaryId: number): Observable<OrderSummary> {
    return this.http
      .get<OrderSummary>(`${this.requestBase}/orders/${orderSummaryId}`)
      .pipe(catchError(this.handleError));
  }

  /**
   * GET /api/request/orders/by-request/{requestId}
   * Returns the order summary for a given original request ID.
   */
  getOrderSummaryByRequestId(requestId: number): Observable<OrderSummary> {
    return this.http
      .get<OrderSummary>(`${this.requestBase}/orders/by-request/${requestId}`)
      .pipe(catchError(this.handleError));
  }

  /**
   * GET /api/request/order-stats
   * Returns aggregate order statistics for the current user (for dashboard cards).
   */
  getOrderStatistics(): Observable<OrderStatistics> {
    return this.http
      .get<OrderStatistics>(`${this.requestBase}/order-stats`)
      .pipe(catchError(this.handleError));
  }

  // ──────────────────────────────────────────────────────────
  // ERROR HANDLING
  // ──────────────────────────────────────────────────────────

  private handleError(err: any): Observable<never> {
    const msg =
      err?.error?.message ||
      err?.error?.Message ||
      err?.message ||
      'An unexpected error occurred.';
    return throwError(() => new Error(msg));
  }
}
