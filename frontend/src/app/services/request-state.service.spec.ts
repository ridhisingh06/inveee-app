import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RequestStateService } from './request-state.service';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { vi } from 'vitest';
import { skip, take } from 'rxjs';

describe('RequestStateService', () => {
  let service: RequestStateService;
  let httpMock: HttpTestingController;
  let mockLogger: any;

  beforeEach(() => {
    mockLogger = {
        log: vi.fn(),
        warn: vi.fn(),
        error: vi.fn(),
        debug: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
          RequestStateService,
          { provide: LoggerService, useValue: mockLogger }
      ]
    });
    service = TestBed.inject(RequestStateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('fetchPendingAdminRequests', () => {
    it('should load data and update subject', () => {
      service.fetchPendingAdminRequests(2, 5, 'test');
      
      const req = httpMock.expectOne(
          `${environment.apiUrl}/requests?status=PendingAdminApproval&page=2&pageSize=5&q=test`
      );
      expect(req.request.method).toBe('GET');

      const mockResponse = {
          data: [{ id: 1 }],
          total: 10,
          totalPages: 2
      };
      
      req.flush(mockResponse);

      service.pendingAdminRequests$.pipe(take(1)).subscribe(state => {
          expect(state.data.length).toBe(1);
          expect(state.total).toBe(10);
          expect(state.currentPage).toBe(2);
      });
      
      expect(mockLogger.log).toHaveBeenCalled();
    });

    it('should handle error', () => {
        service.fetchPendingAdminRequests(1, 10);
        
        const req = httpMock.expectOne(`${environment.apiUrl}/requests?status=PendingAdminApproval&page=1&pageSize=10`);
        req.error(new ProgressEvent('error'));
        
        expect(mockLogger.error).toHaveBeenCalled();
    });
  });

  describe('fetchPendingIssuerRequests', () => {
    it('should load data and update subject', () => {
      service.fetchPendingIssuerRequests(1, 10);
      
      const req = httpMock.expectOne(`${environment.apiUrl}/requests?status=PendingWithIssuer&page=1&pageSize=10`);
      
      const mockResponse = { data: [{ id: 2 }] };
      req.flush(mockResponse);

      service.pendingIssuerRequests$.pipe(take(1)).subscribe(state => {
          expect(state.data.length).toBe(1);
          expect(state.data[0].id).toBe(2);
      });
      
      expect(mockLogger.log).toHaveBeenCalled();
    });
  });

  describe('updateItemStatus', () => {
    it('should update item status in ADMIN workflow', () => {
        // Initialize with data
        service.fetchPendingAdminRequests(1, 10);
        const req = httpMock.expectOne(`${environment.apiUrl}/requests?status=PendingAdminApproval&page=1&pageSize=10`);
        req.flush({
            data: [{ id: 1, items: [{ id: 10, status: 'Pending' }] }]
        });

        // Skip the initial emission and listen for the updated one
        let updatedState: any;
        service.pendingAdminRequests$.subscribe(state => {
           updatedState = state;
        });

        service.updateItemStatus('ADMIN', 1, 10, 'Approved');
        
        expect(updatedState.data[0].items[0].status).toBe('Approved');
    });

    it('should update item status in ISSUER workflow', () => {
        service.fetchPendingIssuerRequests(1, 10);
        const req = httpMock.expectOne(`${environment.apiUrl}/requests?status=PendingWithIssuer&page=1&pageSize=10`);
        req.flush({
            data: [{ id: 2, items: [{ id: 20, status: 'Pending' }] }]
        });

        let updatedState: any;
        service.pendingIssuerRequests$.subscribe(state => {
           updatedState = state;
        });

        service.updateItemStatus('ISSUER', 2, 20, 'Issued');
        
        expect(updatedState.data[0].items[0].status).toBe('Issued');
    });
  });
});
