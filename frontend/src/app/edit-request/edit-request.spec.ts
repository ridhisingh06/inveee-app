import { TestBed, fakeAsync, tick, flush } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError, Subject } from 'rxjs';
import { EditRequestComponent } from './edit-request';
import { RequestService } from '../services/request.service';
import { RefreshService } from '../services/refresh.service';
import { environment } from '../../environments/environment';

// ── Helpers ────────────────────────────────────────────────────────────────

function buildPendingRequest(overrides: Partial<any> = {}) {
  return {
    id: 42,
    status: 'PendingWithIssuer',
    createdAt: new Date().toISOString(),
    items: [
      { id: 1, itemId: 10, itemName: 'Pen', quantityRequested: 5, status: 'PendingWithIssuer' },
      { id: 2, itemId: 20, itemName: 'Stapler', quantityRequested: 2, status: 'PendingWithIssuer' }
    ],
    ...overrides
  };
}

function buildInventory() {
  return [
    { id: 10, name: 'Pen',     category: 'Stationery', availableQuantity: 100 },
    { id: 20, name: 'Stapler', category: 'Stationery', availableQuantity: 50  },
    { id: 30, name: 'Ruler',   category: 'Stationery', availableQuantity: 20  }
  ];
}

// ── Test suite ─────────────────────────────────────────────────────────────

describe('EditRequestComponent', () => {
  let component: EditRequestComponent;
  let httpMock: HttpTestingController;
  let requestService: jasmine.SpyObj<RequestService>;
  let refreshService: jasmine.SpyObj<RefreshService>;
  let router: Router;

  // ActivatedRoute with a controllable paramMap
  let paramMapSubject: Subject<any>;

  beforeEach(async () => {
    paramMapSubject = new Subject();

    requestService = jasmine.createSpyObj('RequestService', [
      'getRequestById',
      'updateRequest',
      'isRequestEditable'
    ]);

    refreshService = jasmine.createSpyObj('RefreshService', ['notifyRequests']);

    await TestBed.configureTestingModule({
      imports: [
        EditRequestComponent,
        HttpClientTestingModule,
        RouterTestingModule
      ],
      providers: [
        { provide: RequestService, useValue: requestService },
        { provide: RefreshService, useValue: refreshService },
        {
          provide: ActivatedRoute,
          useValue: { paramMap: paramMapSubject.asObservable() }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(EditRequestComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);

    spyOn(router, 'navigate');
  });

  afterEach(() => {
    httpMock.verify();
  });

  // ── 1. Init ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set loading=true and errorMsg="" on init', () => {
    expect(component.loading).toBeTrue();
    expect(component.errorMsg).toBe('');
  });

  it('should set errorMsg when no id param is provided', () => {
    paramMapSubject.next({ get: () => null });
    expect(component.errorMsg).toBe('No request ID provided.');
    expect(component.loading).toBeFalse();
  });

  // ── 2. loadAll — happy path ────────────────────────────────────────────────

  it('should populate lines when request is PendingWithIssuer and all items pending', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));

    paramMapSubject.next({ get: () => '42' });

    // Flush the parallel inventory call
    const invReq = httpMock.expectOne(`${environment.apiUrl}/inventory`);
    invReq.flush(buildInventory());

    tick();

    expect(component.loading).toBeFalse();
    expect(component.errorMsg).toBe('');
    expect(component.lines.length).toBe(2);
    expect(component.lines[0].item.name).toBe('Pen');
    expect(component.lines[0].qty).toBe(5);
    expect(component.lines[1].item.name).toBe('Stapler');
    expect(component.lines[1].qty).toBe(2);
  }));

  it('should populate allItems from inventory response', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });

    const invReq = httpMock.expectOne(`${environment.apiUrl}/inventory`);
    invReq.flush(buildInventory());
    tick();

    expect(component.allItems.length).toBe(3);
    expect(component.allItems[2].name).toBe('Ruler');
  }));

  // ── 3. loadAll — non-editable states ────────────────────────────────────

  it('should set errorMsg when request status is PendingAdminApproval', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(
      buildPendingRequest({ status: 'PendingAdminApproval' })
    ));
    paramMapSubject.next({ get: () => '42' });

    const invReq = httpMock.expectOne(`${environment.apiUrl}/inventory`);
    invReq.flush(buildInventory());
    tick();

    expect(component.loading).toBeFalse();
    expect(component.errorMsg).toContain('PendingAdminApproval');
    expect(component.lines.length).toBe(0);
  }));

  it('should set errorMsg when request status is Approved', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(
      buildPendingRequest({ status: 'Approved' })
    ));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    expect(component.errorMsg).toContain('Approved');
    expect(component.lines.length).toBe(0);
  }));

  it('should set errorMsg when one item has been touched by issuer', fakeAsync(() => {
    const req = buildPendingRequest({
      items: [
        { id: 1, itemId: 10, itemName: 'Pen', quantityRequested: 5, status: 'PendingAdminApproval' },
        { id: 2, itemId: 20, itemName: 'Stapler', quantityRequested: 2, status: 'PendingWithIssuer' }
      ]
    });
    requestService.getRequestById.and.returnValue(of(req));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    expect(component.loading).toBeFalse();
    expect(component.errorMsg).toContain('issuer');
    expect(component.lines.length).toBe(0);
  }));

  it('should set errorMsg when item status is "Requested" (legacy alias)', fakeAsync(() => {
    // "Requested" maps to pendingwithissuer — component should treat it as editable
    const req = buildPendingRequest({
      status: 'Requested',
      items: [
        { id: 1, itemId: 10, itemName: 'Pen', quantityRequested: 3, status: 'Requested' }
      ]
    });
    requestService.getRequestById.and.returnValue(of(req));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    expect(component.loading).toBeFalse();
    expect(component.errorMsg).toBe('');
    expect(component.lines.length).toBe(1);
  }));

  // ── 4. loadAll — network errors ──────────────────────────────────────────

  it('should set errorMsg when getRequestById returns 404', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(
      throwError(() => ({ message: 'Request not found or access denied' }))
    );
    paramMapSubject.next({ get: () => '99' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    expect(component.loading).toBeFalse();
    expect(component.errorMsg).toContain('not found');
  }));

  it('should set errorMsg with timeout message after 15 seconds', fakeAsync(() => {
    // getRequestById never resolves — simulates a hanging call
    requestService.getRequestById.and.returnValue(new Subject<any>().asObservable());
    paramMapSubject.next({ get: () => '42' });

    // Inventory resolves immediately but request hangs
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);

    tick(15001); // advance past the 15 s timeout

    expect(component.loading).toBeFalse();
    expect(component.errorMsg).toContain('timed out');
  }));

  // ── 5. changeQty ─────────────────────────────────────────────────────────

  it('should increment qty', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    component.changeQty(10, 5, 1);
    expect(component.lines[0].qty).toBe(6);
  }));

  it('should decrement qty', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    component.changeQty(10, 5, -1);
    expect(component.lines[0].qty).toBe(4);
  }));

  it('should NOT go below qty 1', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    component.changeQty(10, 1, -1); // try to go to 0
    expect(component.lines[0].qty).toBe(1);
  }));

  // ── 6. removeLine ────────────────────────────────────────────────────────

  it('should remove line by itemId', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    expect(component.lines.length).toBe(2);
    component.removeLine(10);
    expect(component.lines.length).toBe(1);
    expect(component.lines[0].item.id).toBe(20);
  }));

  it('totalUnits should reflect current lines', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    // 5 + 2 = 7
    expect(component.totalUnits).toBe(7);

    component.changeQty(10, 5, 1); // pen → 6
    expect(component.totalUnits).toBe(8);

    component.removeLine(20); // remove stapler
    expect(component.totalUnits).toBe(6);
  }));

  // ── 7. addItem ───────────────────────────────────────────────────────────

  it('should add a new item not already in lines', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush(buildInventory());
    tick();

    component.addItem({ id: 30, name: 'Ruler', category: 'Stationery' } as any);
    expect(component.lines.length).toBe(3);
    expect(component.lines[2].item.name).toBe('Ruler');
    expect(component.lines[2].qty).toBe(1);
  }));

  it('should increment qty when adding an item already in lines', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    component.addItem({ id: 10, name: 'Pen', category: 'Stationery' } as any);
    expect(component.lines.length).toBe(2);
    expect(component.lines[0].qty).toBe(6); // was 5, now 6
  }));

  // ── 8. filterItems / search ──────────────────────────────────────────────

  it('should show up to 50 items when no search term', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush(buildInventory());
    tick();

    component.toggleItemSearch(); // opens search panel
    expect(component.filteredItems.length).toBe(3); // only 3 in test data
  }));

  it('should filter items by search term (case-insensitive)', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush(buildInventory());
    tick();

    component.toggleItemSearch();
    component.searchText = 'PEN';
    component.filterItems();
    expect(component.filteredItems.length).toBe(1);
    expect(component.filteredItems[0].name).toBe('Pen');
  }));

  it('should return empty filteredItems when search term matches nothing', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush(buildInventory());
    tick();

    component.toggleItemSearch();
    component.searchText = 'xxxxxxxxxxx';
    component.filterItems();
    expect(component.filteredItems.length).toBe(0);
  }));

  // ── 9. saveChanges — validation guards ──────────────────────────────────

  it('should set errorMsg when saveChanges called with empty lines', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    component.lines = [];
    component.saveChanges();
    expect(component.errorMsg).toBe('Request must contain at least one item.');
    expect(requestService.updateRequest).not.toHaveBeenCalled();
  }));

  it('should not call updateRequest when requestId is null', () => {
    component.requestId = null;
    component.saveChanges();
    expect(requestService.updateRequest).not.toHaveBeenCalled();
  });

  // ── 10. saveChanges — happy path ─────────────────────────────────────────

  it('should call updateRequest with correct payload on saveChanges', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    requestService.updateRequest.and.returnValue(
      of({ success: true, message: 'Request updated successfully.', requestId: 42 })
    );

    component.saveChanges();
    tick();

    expect(requestService.updateRequest).toHaveBeenCalledOnceWith(42, {
      items: [
        { itemId: 10, quantity: 5 },
        { itemId: 20, quantity: 2 }
      ]
    });
  }));

  it('should show successMsg after successful save', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    requestService.updateRequest.and.returnValue(
      of({ success: true, message: 'Request updated successfully.', requestId: 42 })
    );

    component.saveChanges();
    tick();

    expect(component.successMsg).toBe('Request updated successfully!');
    expect(component.submitting).toBeFalse();
  }));

  it('should call refresh.notifyRequests after successful save', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    requestService.updateRequest.and.returnValue(of({ success: true }));
    component.saveChanges();
    tick();

    expect(refreshService.notifyRequests).toHaveBeenCalledOnce();
  }));

  it('should navigate to /user-dashboard/my-requests 1 second after successful save', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    requestService.updateRequest.and.returnValue(of({ success: true }));
    component.saveChanges();
    tick(1000);

    expect(router.navigate).toHaveBeenCalledWith(['/user-dashboard/my-requests']);
  }));

  it('should NOT navigate before 1 second has elapsed', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    requestService.updateRequest.and.returnValue(of({ success: true }));
    component.saveChanges();
    tick(500); // only half the delay

    expect(router.navigate).not.toHaveBeenCalled();
    tick(500); // complete the delay
    flush();
  }));

  // ── 11. saveChanges — error path ─────────────────────────────────────────

  it('should set errorMsg and clear submitting flag on updateRequest error', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    requestService.updateRequest.and.returnValue(
      throwError(() => ({ message: 'The issuer has started processing this request.' }))
    );

    component.saveChanges();
    tick();

    expect(component.errorMsg).toBe('The issuer has started processing this request.');
    expect(component.submitting).toBeFalse();
    expect(refreshService.notifyRequests).not.toHaveBeenCalled();
  }));

  it('should use fallback error message when backend provides none', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    requestService.updateRequest.and.returnValue(throwError(() => ({})));
    component.saveChanges();
    tick();

    expect(component.errorMsg).toBe('Failed to update request.');
  }));

  // ── 12. cancel ───────────────────────────────────────────────────────────

  it('should navigate to my-requests on cancel()', () => {
    component.cancel();
    expect(router.navigate).toHaveBeenCalledWith(['/user-dashboard/my-requests']);
  });

  // ── 13. payload construction — edge cases ────────────────────────────────

  it('should send itemId as a number even if item.id is a string', fakeAsync(() => {
    const reqData = buildPendingRequest({
      items: [{ id: 1, itemId: '10', itemName: 'Pen', quantityRequested: 2, status: 'PendingWithIssuer' }]
    });
    requestService.getRequestById.and.returnValue(of(reqData));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    requestService.updateRequest.and.returnValue(of({ success: true }));
    component.saveChanges();
    tick();

    const callArg = (requestService.updateRequest as jasmine.Spy).calls.mostRecent().args[1];
    expect(typeof callArg.items[0].itemId).toBe('number');
    expect(callArg.items[0].itemId).toBe(10);
    flush();
  }));

  it('should send updated quantities after changeQty', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    component.changeQty(10, 5, 1); // pen 5 → 6
    component.changeQty(10, 6, 1); // pen 6 → 7

    requestService.updateRequest.and.returnValue(of({ success: true }));
    component.saveChanges();
    tick();

    const callArg = (requestService.updateRequest as jasmine.Spy).calls.mostRecent().args[1];
    const pen = callArg.items.find((i: any) => i.itemId === 10);
    expect(pen.quantity).toBe(7);
    flush();
  }));

  it('should exclude removed items from the update payload', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush([]);
    tick();

    component.removeLine(20); // remove Stapler

    requestService.updateRequest.and.returnValue(of({ success: true }));
    component.saveChanges();
    tick();

    const callArg = (requestService.updateRequest as jasmine.Spy).calls.mostRecent().args[1];
    expect(callArg.items.length).toBe(1);
    expect(callArg.items[0].itemId).toBe(10);
    flush();
  }));

  it('should include newly added items in the update payload', fakeAsync(() => {
    requestService.getRequestById.and.returnValue(of(buildPendingRequest()));
    paramMapSubject.next({ get: () => '42' });
    httpMock.expectOne(`${environment.apiUrl}/inventory`).flush(buildInventory());
    tick();

    component.addItem({ id: 30, name: 'Ruler', category: 'Stationery' } as any);

    requestService.updateRequest.and.returnValue(of({ success: true }));
    component.saveChanges();
    tick();

    const callArg = (requestService.updateRequest as jasmine.Spy).calls.mostRecent().args[1];
    expect(callArg.items.length).toBe(3);
    expect(callArg.items.some((i: any) => i.itemId === 30)).toBeTrue();
    flush();
  }));

  // ── 14. memory leak / destroy ─────────────────────────────────────────────

  it('should complete destroy$ on ngOnDestroy', () => {
    const destroyNext = spyOn((component as any).destroy$, 'next');
    const destroyComplete = spyOn((component as any).destroy$, 'complete');
    component.ngOnDestroy();
    expect(destroyNext).toHaveBeenCalled();
    expect(destroyComplete).toHaveBeenCalled();
  });
});
