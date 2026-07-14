import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RequestService } from './request.service';
import { environment } from '../../environments/environment';
import { CreateRequestPayload } from '../models/request.model';

describe('RequestService', () => {
  let service: RequestService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [RequestService]
    });
    service = TestBed.inject(RequestService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get my requests', () => {
    service.getMyRequests(2, 10).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/requests?pageNumber=2&pageSize=10`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should get request by id', () => {
    service.getRequestById(1).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/requests/1`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('should create request', () => {
    const payload: CreateRequestPayload = { categoryId: 1, items: [{ itemId: 1, quantity: 2 }] };
    service.createRequest(payload).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/requests`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 1, message: 'Created' });
  });

  it('should confirm received', () => {
    service.confirmReceived(1).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/requests/1/receive`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ message: 'Confirmed' });
  });

  it('should cancel request', () => {
    service.cancelRequest(1).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/requests/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ message: 'Canceled' });
  });

  it('should check if can request', () => {
    service.canRequest().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/requests/can-request`);
    expect(req.request.method).toBe('GET');
    req.flush({ canRequest: true, message: '' });
  });

  // ── updateRequest ─────────────────────────────────────────────────────────

  it('should PUT to /requests/:id on updateRequest', () => {
    const payload = { items: [{ itemId: 1, quantity: 3 }] };
    let result: any;
    service.updateRequest(42, payload).subscribe(r => (result = r));

    const req = httpMock.expectOne(`${environment.apiUrl}/requests/42`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({ success: true, message: 'Request updated successfully.', requestId: 42 });

    expect(result.success).toBeTrue();
    expect(result.requestId).toBe(42);
  });

  it('should emit error from updateRequest when backend returns 400', () => {
    const payload = { items: [{ itemId: 1, quantity: 3 }] };
    let errorMsg = '';
    service.updateRequest(99, payload).subscribe({
      error: (e) => (errorMsg = e.message)
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/requests/99`);
    req.flush({ message: 'Request not found.' }, { status: 400, statusText: 'Bad Request' });

    expect(errorMsg).toBe('Request not found.');
  });

  it('should emit error from updateRequest when backend returns 500', () => {
    let errorMsg = '';
    service.updateRequest(1, { items: [] }).subscribe({
      error: (e) => (errorMsg = e.message)
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/requests/1`);
    req.flush({}, { status: 500, statusText: 'Server Error' });

    expect(errorMsg).toBeTruthy();
  });

  // ── isRequestEditable ──────────────────────────────────────────────────────

  it('should GET /requests/:id/editable', () => {
    let result: any;
    service.isRequestEditable(5).subscribe(r => (result = r));

    const req = httpMock.expectOne(`${environment.apiUrl}/requests/5/editable`);
    expect(req.request.method).toBe('GET');
    req.flush({ editable: true, reason: 'Request is editable.' });

    expect(result.editable).toBeTrue();
    expect(result.reason).toBe('Request is editable.');
  });

  it('should return editable=false when issuer has started processing', () => {
    let result: any;
    service.isRequestEditable(7).subscribe(r => (result = r));

    const req = httpMock.expectOne(`${environment.apiUrl}/requests/7/editable`);
    req.flush({ editable: false, reason: 'The issuer has started processing this request.' });

    expect(result.editable).toBeFalse();
  });
});
