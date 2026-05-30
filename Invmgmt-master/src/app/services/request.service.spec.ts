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
});
