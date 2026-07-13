import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

/**
 * RefreshService
 *
 * Centralised, typed signal bus for cross-component UI synchronisation.
 *
 * Consumers call `service.notify<Domain>()` after a successful mutation.
 * Listeners call `service.<domain>$.subscribe(...)` to react immediately.
 *
 * Rules:
 *  - Use Subject (not BehaviorSubject) — we want to fire-and-forget, not
 *    replay the last event to late subscribers.
 *  - Every subscriber MUST unsubscribe via takeUntil(destroy$) to avoid
 *    memory leaks.
 *  - Do NOT store state here; state lives in the domain services / components.
 */
@Injectable({ providedIn: 'root' })
export class RefreshService {

  // ── User / My-Requests domain ─────────────────────────────────────────────
  /** Emitted after a request is created, updated, cancelled, or received. */
  private readonly _requests$ = new Subject<void>();
  readonly requests$ = this._requests$.asObservable();

  notifyRequests(): void { this._requests$.next(); }

  // ── Order-History domain ──────────────────────────────────────────────────
  /** Emitted after a user confirms receipt (order summary created). */
  private readonly _orders$ = new Subject<void>();
  readonly orders$ = this._orders$.asObservable();

  notifyOrders(): void { this._orders$.next(); }

  // ── Issuer domain ─────────────────────────────────────────────────────────
  /** Emitted after issuer submits a partial issue. */
  private readonly _issuer$ = new Subject<void>();
  readonly issuer$ = this._issuer$.asObservable();

  notifyIssuer(): void { this._issuer$.next(); }

  // ── Admin-workflow domain (partial approvals) ─────────────────────────────
  /** Emitted after admin approves/rejects a pending-approval group. */
  private readonly _adminApproval$ = new Subject<void>();
  readonly adminApproval$ = this._adminApproval$.asObservable();

  notifyAdminApproval(): void { this._adminApproval$.next(); }

  // ── Registration domain ───────────────────────────────────────────────────
  /** Emitted after admin approves or rejects a registration request. */
  private readonly _registration$ = new Subject<void>();
  readonly registration$ = this._registration$.asObservable();

  notifyRegistration(): void { this._registration$.next(); }

  // ── Inventory domain ──────────────────────────────────────────────────────
  /** Emitted after any inventory CRUD or stock change. */
  private readonly _inventory$ = new Subject<void>();
  readonly inventory$ = this._inventory$.asObservable();

  notifyInventory(): void { this._inventory$.next(); }
}
