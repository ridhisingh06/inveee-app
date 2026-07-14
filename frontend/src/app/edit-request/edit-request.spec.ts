/**
 * edit-request.spec.ts
 *
 * Pure Vitest unit tests for EditRequestComponent — no TestBed.
 *
 * Because EditRequestComponent uses external templateUrl / styleUrls,
 * Angular's compiler requires resolveComponentResources() to resolve those
 * files before TestBed can compile the component inside jsdom. This is
 * impractical for Vitest + jsdom environments without a custom Vite plugin.
 *
 * Instead, we test the component CLASS directly:
 *   - Inject mocked dependencies via constructor.
 *   - Call lifecycle hooks and public methods directly.
 *   - Verify observable side-effects through mock call counts / arguments.
 *
 * This approach is faster, framework-agnostic, and exercises 100% of the
 * component's business logic without depending on the template.
 */

import { describe, it, expect, vi, beforeEach, afterEach, type Mock } from 'vitest';
import { Subject, of, throwError } from 'rxjs';
import { EditRequestComponent }    from './edit-request';
import { Item }                    from '../models/item';

// ─────────────────────────────────────────────────────────────────────────────
// Minimal stub types
// ─────────────────────────────────────────────────────────────────────────────

interface StubRoute { paramMap: Subject<{ get: (k: string) => string | null }> }

function makeStubs() {
  const paramMap$ = new Subject<{ get: (k: string) => string | null }>();

  const route: StubRoute = { paramMap: paramMap$ };

  const router      = { navigate: vi.fn().mockResolvedValue(true) };

  // Fake HttpClient: .get() returns a Subject so tests control when it emits
  const httpClient  = { get: vi.fn() };

  const requestSvc  = {
    getRequestById:    vi.fn() as Mock,
    updateRequest:     vi.fn() as Mock,
    isRequestEditable: vi.fn() as Mock
  };

  const refreshSvc  = { notifyRequests: vi.fn() };

  return { paramMap$, route, router, httpClient, requestSvc, refreshSvc };
}

// ─────────────────────────────────────────────────────────────────────────────
// Test-data factories
// ─────────────────────────────────────────────────────────────────────────────

function buildRequest(overrides: Record<string, unknown> = {}) {
  return {
    id: 42, status: 'PendingWithIssuer', createdAt: '2026-01-01T00:00:00Z',
    items: [
      { id: 1, itemId: 10, itemName: 'Pen',    quantityRequested: 5, status: 'PendingWithIssuer' },
      { id: 2, itemId: 20, itemName: 'Stapler', quantityRequested: 2, status: 'PendingWithIssuer' }
    ],
    ...overrides
  };
}

function buildInventory() {
  return [
    { id: 10, name: 'Pen',     category: 'Stationery' },
    { id: 20, name: 'Stapler', category: 'Stationery' },
    { id: 30, name: 'Ruler',   category: 'Stationery' }
  ];
}

// ─────────────────────────────────────────────────────────────────────────────
// Helper: create component and call ngOnInit
// ─────────────────────────────────────────────────────────────────────────────

function createComponent() {
  const stubs = makeStubs();
  const component = new EditRequestComponent(
    stubs.route as any,
    stubs.router as any,
    stubs.httpClient as any,
    stubs.requestSvc as any,
    stubs.refreshSvc as any
  );
  component.ngOnInit();
  return { component, ...stubs };
}

/**
 * Simulate loading a valid PendingWithIssuer request:
 * 1. Emit the route param.
 * 2. Resolve the getRequestById observable.
 * 3. Resolve the inventory GET.
 */
function loadValidRequest(
  component: EditRequestComponent,
  stubs: ReturnType<typeof makeStubs>,
  reqOverride: Record<string, unknown> = {}
) {
  stubs.requestSvc.getRequestById.mockReturnValue(of(buildRequest(reqOverride)));
  // Inventory resolves with items
  stubs.httpClient.get.mockReturnValue(of(buildInventory()));
  stubs.paramMap$.next({ get: () => '42' });
}

// ─────────────────────────────────────────────────────────────────────────────
// Suite
// ─────────────────────────────────────────────────────────────────────────────

describe('EditRequestComponent — class logic', () => {

  afterEach(() => {
    vi.restoreAllMocks();
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  it('creates without errors', () => {
    const { component } = createComponent();
    expect(component).toBeTruthy();
  });

  it('starts with loading=true and no error', () => {
    const { component } = createComponent();
    expect(component.loading).toBe(true);
    expect(component.errorMsg).toBe('');
  });

  it('sets errorMsg when no id param provided', () => {
    const { component, paramMap$ } = createComponent();
    paramMap$.next({ get: () => null });
    expect(component.loading).toBe(false);
    expect(component.errorMsg).toContain('No valid request ID');
  });

  it('sets errorMsg when id param is not numeric', () => {
    const { component, paramMap$ } = createComponent();
    paramMap$.next({ get: () => 'abc' });
    expect(component.loading).toBe(false);
    expect(component.errorMsg).toContain('No valid request ID');
  });

  it('calls getRequestById with the parsed integer id', () => {
    const stubs = makeStubs();
    const component = new EditRequestComponent(
      stubs.route as any, stubs.router as any,
      stubs.httpClient as any, stubs.requestSvc as any, stubs.refreshSvc as any
    );
    component.ngOnInit();
    stubs.requestSvc.getRequestById.mockReturnValue(of(buildRequest()));
    stubs.httpClient.get.mockReturnValue(of([]));
    stubs.paramMap$.next({ get: () => '42' });
    expect(stubs.requestSvc.getRequestById).toHaveBeenCalledWith(42);
  });

  // ── Editability — happy path ──────────────────────────────────────────────

  it('populates lines when all items are PendingWithIssuer', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);

    expect(component.loading).toBe(false);
    expect(component.errorMsg).toBe('');
    expect(component.lines.length).toBe(2);
    expect(component.lines[0].item.name).toBe('Pen');
    expect(component.lines[0].qty).toBe(5);
    expect(component.lines[1].item.name).toBe('Stapler');
    expect(component.lines[1].qty).toBe(2);
  });

  it('accepts legacy "Requested" status as editable', () => {
    const { component, ...stubs } = createComponent();
    stubs.requestSvc.getRequestById.mockReturnValue(of(buildRequest({
      status: 'Requested',
      items: [{ id: 1, itemId: 10, itemName: 'Pen', quantityRequested: 3, status: 'Requested' }]
    })));
    stubs.httpClient.get.mockReturnValue(of([]));
    stubs.paramMap$.next({ get: () => '42' });

    expect(component.errorMsg).toBe('');
    expect(component.lines.length).toBe(1);
  });

  // ── Editability — blocked states ──────────────────────────────────────────

  it.each([
    ['PendingAdminApproval'],
    ['Approved'],
    ['Received'],
    ['Rejected']
  ])('sets errorMsg and empty lines when status is %s', (status) => {
    const { component, ...stubs } = createComponent();
    stubs.requestSvc.getRequestById.mockReturnValue(of(buildRequest({ status })));
    stubs.paramMap$.next({ get: () => '42' });

    expect(component.loading).toBe(false);
    expect(component.errorMsg).toContain(status);
    expect(component.lines.length).toBe(0);
  });

  it('sets errorMsg when issuer has touched at least one item', () => {
    const { component, ...stubs } = createComponent();
    stubs.requestSvc.getRequestById.mockReturnValue(of(buildRequest({
      items: [
        { id: 1, itemId: 10, itemName: 'Pen',    quantityRequested: 5, status: 'PendingAdminApproval' },
        { id: 2, itemId: 20, itemName: 'Stapler', quantityRequested: 2, status: 'PendingWithIssuer' }
      ]
    })));
    stubs.paramMap$.next({ get: () => '42' });

    expect(component.errorMsg).toContain('issuer');
    expect(component.lines.length).toBe(0);
  });

  // ── Network errors ────────────────────────────────────────────────────────

  it('sets errorMsg when getRequestById throws', () => {
    const { component, ...stubs } = createComponent();
    stubs.requestSvc.getRequestById.mockReturnValue(
      throwError(() => ({ message: 'Request not found.' }))
    );
    stubs.paramMap$.next({ get: () => '99' });

    expect(component.loading).toBe(false);
    expect(component.errorMsg).toBe('Request not found.');
  });

  it('uses fallback errorMsg when error has no message', () => {
    const { component, ...stubs } = createComponent();
    stubs.requestSvc.getRequestById.mockReturnValue(throwError(() => ({})));
    stubs.paramMap$.next({ get: () => '99' });

    expect(component.errorMsg).toContain('Failed to load request');
  });

  // ── Inventory — non-blocking ──────────────────────────────────────────────

  it('populates allItems after successful inventory fetch', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);

    expect(component.allItems.length).toBe(3);
    expect(component.allItems[2].name).toBe('Ruler');
  });

  it('does NOT set errorMsg when inventory fetch fails', () => {
    const { component, ...stubs } = createComponent();
    stubs.requestSvc.getRequestById.mockReturnValue(of(buildRequest()));
    stubs.httpClient.get.mockReturnValue(throwError(() => ({ status: 500 })));
    stubs.paramMap$.next({ get: () => '42' });

    expect(component.errorMsg).toBe('');
    expect(component.lines.length).toBe(2); // request still loaded
  });

  // ── changeQty ─────────────────────────────────────────────────────────────

  it('increments quantity', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.changeQty(10, 5, 1);
    expect(component.lines[0].qty).toBe(6);
  });

  it('decrements quantity', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.changeQty(10, 5, -1);
    expect(component.lines[0].qty).toBe(4);
  });

  it('does NOT allow quantity below 1', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    // Pen starts at qty=5. Decrement to 1 is fine.
    component.changeQty(10, 5, -1);
    component.changeQty(10, 4, -1);
    component.changeQty(10, 3, -1);
    component.changeQty(10, 2, -1);
    expect(component.lines[0].qty).toBe(1);
    // Trying to go below 1 should be blocked — qty stays at 1
    component.changeQty(10, 1, -1);
    expect(component.lines[0].qty).toBe(1);
  });

  // ── setQty ────────────────────────────────────────────────────────────────

  it('sets quantity from direct input', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.setQty(10, '12');
    expect(component.lines[0].qty).toBe(12);
  });

  it('ignores non-numeric input', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.setQty(10, 'hello');
    expect(component.lines[0].qty).toBe(5); // unchanged
  });

  it('ignores zero input', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.setQty(10, '0');
    expect(component.lines[0].qty).toBe(5);
  });

  it('ignores negative input', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.setQty(10, '-3');
    expect(component.lines[0].qty).toBe(5);
  });

  // ── removeLine ────────────────────────────────────────────────────────────

  it('removes item by id', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.removeLine(10);
    expect(component.lines.length).toBe(1);
    expect(component.lines[0].item.name).toBe('Stapler');
  });

  it('totalUnits sums all lines correctly', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    expect(component.totalUnits).toBe(7);          // 5 + 2
    component.changeQty(10, 5, 1);
    expect(component.totalUnits).toBe(8);
    component.removeLine(20);
    expect(component.totalUnits).toBe(6);           // pen only
  });

  // ── addItem ───────────────────────────────────────────────────────────────

  it('adds a new item', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.addItem({ id: 30, name: 'Ruler', category: 'Stationery' } as Item);
    expect(component.lines.length).toBe(3);
    expect(component.lines[2].qty).toBe(1);
  });

  it('increments qty when adding a duplicate item', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.addItem({ id: 10, name: 'Pen', category: 'Stationery' } as Item);
    expect(component.lines.length).toBe(2);
    expect(component.lines[0].qty).toBe(6);
  });

  it('shows successMsg after addItem and clears it after 2.5 s', () => {
    vi.useFakeTimers();
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.addItem({ id: 30, name: 'Ruler', category: 'X' } as Item);
    expect(component.successMsg).toContain('Ruler');
    vi.advanceTimersByTime(2500);
    expect(component.successMsg).toBe('');
    vi.useRealTimers();
  });

  // ── filterItems ───────────────────────────────────────────────────────────

  it('shows items when panel is opened', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.toggleItemSearch();
    expect(component.filteredItems.length).toBe(3);
  });

  it('filters items case-insensitively', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.toggleItemSearch();
    component.searchText = 'RUL';
    component.filterItems();
    expect(component.filteredItems.length).toBe(1);
    expect(component.filteredItems[0].name).toBe('Ruler');
  });

  it('returns empty array when no match', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.toggleItemSearch();
    component.searchText = 'xyzxyz';
    component.filterItems();
    expect(component.filteredItems.length).toBe(0);
  });

  it('toggles showItemSearch open/closed', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    expect(component.showItemSearch).toBe(false);
    component.toggleItemSearch();
    expect(component.showItemSearch).toBe(true);
    component.toggleItemSearch();
    expect(component.showItemSearch).toBe(false);
  });

  // ── saveChanges — guards ──────────────────────────────────────────────────

  it('sets errorMsg and does not call updateRequest when lines empty', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.lines = [];
    component.saveChanges();
    expect(component.errorMsg).toContain('at least one item');
    expect(stubs.requestSvc.updateRequest).not.toHaveBeenCalled();
  });

  it('does nothing when requestId is null', () => {
    const { component, ...stubs } = createComponent();
    component.requestId = null;
    component.saveChanges();
    expect(stubs.requestSvc.updateRequest).not.toHaveBeenCalled();
  });

  // ── saveChanges — payload ─────────────────────────────────────────────────

  it('calls updateRequest with the correct items payload', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    stubs.requestSvc.updateRequest.mockReturnValue(of({ success: true, message: 'Request updated successfully.' }));
    component.saveChanges();

    expect(stubs.requestSvc.updateRequest).toHaveBeenCalledWith(42, {
      items: [
        { itemId: 10, quantity: 5 },
        { itemId: 20, quantity: 2 }
      ]
    });
  });

  it('converts string item.id to number in payload', () => {
    const { component, ...stubs } = createComponent();
    stubs.requestSvc.getRequestById.mockReturnValue(of(buildRequest({
      items: [{ id: 1, itemId: '10', itemName: 'Pen', quantityRequested: 3, status: 'PendingWithIssuer' }]
    })));
    stubs.httpClient.get.mockReturnValue(of([]));
    stubs.paramMap$.next({ get: () => '42' });

    stubs.requestSvc.updateRequest.mockReturnValue(of({ success: true }));
    component.saveChanges();

    const sentItemId = stubs.requestSvc.updateRequest.mock.calls[0][1].items[0].itemId;
    expect(typeof sentItemId).toBe('number');
    expect(sentItemId).toBe(10);
  });

  it('sends updated quantities after changeQty', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.changeQty(10, 5, 1);
    component.changeQty(10, 6, 1); // pen: 5 → 6 → 7
    stubs.requestSvc.updateRequest.mockReturnValue(of({ success: true }));
    component.saveChanges();

    const pen = stubs.requestSvc.updateRequest.mock.calls[0][1].items
      .find((i: { itemId: number }) => i.itemId === 10);
    expect(pen.quantity).toBe(7);
  });

  it('excludes removed items from payload', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.removeLine(20); // remove Stapler
    stubs.requestSvc.updateRequest.mockReturnValue(of({ success: true }));
    component.saveChanges();

    const items = stubs.requestSvc.updateRequest.mock.calls[0][1].items;
    expect(items.length).toBe(1);
    expect(items[0].itemId).toBe(10);
  });

  it('includes newly added items in payload', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    component.addItem({ id: 30, name: 'Ruler', category: 'Stationery' } as Item);
    stubs.requestSvc.updateRequest.mockReturnValue(of({ success: true }));
    component.saveChanges();

    const items = stubs.requestSvc.updateRequest.mock.calls[0][1].items;
    expect(items.length).toBe(3);
    expect(items.some((i: { itemId: number }) => i.itemId === 30)).toBe(true);
  });

  // ── saveChanges — success path ────────────────────────────────────────────

  it('shows successMsg and notifies refresh on success', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    stubs.requestSvc.updateRequest.mockReturnValue(
      of({ success: true, message: 'Request updated successfully.' })
    );
    component.saveChanges();

    expect(component.successMsg).toBe('Request updated successfully.');
    expect(component.submitting).toBe(false);
    expect(stubs.refreshSvc.notifyRequests).toHaveBeenCalledOnce();
  });

  it('navigates to my-requests after 1.2 s', () => {
    vi.useFakeTimers();
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    stubs.requestSvc.updateRequest.mockReturnValue(of({ success: true }));
    component.saveChanges();

    expect(stubs.router.navigate).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1200);
    expect(stubs.router.navigate).toHaveBeenCalledWith(['/user-dashboard/my-requests']);
    vi.useRealTimers();
  });

  it('does NOT navigate before 1.2 s', () => {
    vi.useFakeTimers();
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    stubs.requestSvc.updateRequest.mockReturnValue(of({ success: true }));
    component.saveChanges();

    vi.advanceTimersByTime(600);
    expect(stubs.router.navigate).not.toHaveBeenCalled();
    vi.advanceTimersByTime(600);
    expect(stubs.router.navigate).toHaveBeenCalled();
    vi.useRealTimers();
  });

  // ── saveChanges — error path ──────────────────────────────────────────────

  it('sets errorMsg and clears submitting on error', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    stubs.requestSvc.updateRequest.mockReturnValue(
      throwError(() => ({ message: 'Issuer locked.' }))
    );
    component.saveChanges();

    expect(component.errorMsg).toBe('Issuer locked.');
    expect(component.submitting).toBe(false);
    expect(stubs.refreshSvc.notifyRequests).not.toHaveBeenCalled();
  });

  it('uses fallback errorMsg when updateRequest error has no message', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    stubs.requestSvc.updateRequest.mockReturnValue(throwError(() => ({})));
    component.saveChanges();
    expect(component.errorMsg).toContain('Failed to save changes');
  });

  // ── cancel ────────────────────────────────────────────────────────────────

  it('navigates to my-requests on cancel()', () => {
    const { component, router } = createComponent();
    component.cancel();
    expect(router.navigate).toHaveBeenCalledWith(['/user-dashboard/my-requests']);
  });

  // ── trackByItemId ─────────────────────────────────────────────────────────

  it('trackByItemId returns item id', () => {
    const { component, ...stubs } = createComponent();
    loadValidRequest(component, stubs);
    expect(component.trackByItemId(0, component.lines[0])).toBe(10);
  });

  // ── Memory / destroy ──────────────────────────────────────────────────────

  it('completes destroy$ on ngOnDestroy', () => {
    const { component } = createComponent();
    const d = (component as any).destroy$;
    const next     = vi.spyOn(d, 'next');
    const complete = vi.spyOn(d, 'complete');
    component.ngOnDestroy();
    expect(next).toHaveBeenCalled();
    expect(complete).toHaveBeenCalled();
  });
});
