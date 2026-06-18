import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from './auth-interceptor';
import { Router } from '@angular/router';
import { vi } from 'vitest';

describe('AuthInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let mockRouter: any;
  let mockLocalStorage: any;

  beforeEach(() => {
    mockRouter = {
      navigate: vi.fn()
    };

    mockLocalStorage = {
      getItem: vi.fn(),
      removeItem: vi.fn()
    };
    vi.stubGlobal('localStorage', mockLocalStorage);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: mockRouter }
      ]
    });
    
    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    httpMock.verify();
  });

  it('should add Authorization header if token exists', () => {
    mockLocalStorage.getItem.mockReturnValue('test-token');

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBe(true);
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush({});
  });

  it('should not add Authorization header if no token', () => {
    mockLocalStorage.getItem.mockReturnValue(null);

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should clear storage and navigate to login on 401', () => {
    mockLocalStorage.getItem.mockReturnValue('test-token');

    httpClient.get('/api/test').subscribe({
        error: () => {} // ignore error in test
    });

    const req = httpMock.expectOne('/api/test');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(mockLocalStorage.removeItem).toHaveBeenCalledWith('token');
    expect(mockLocalStorage.removeItem).toHaveBeenCalledWith('role');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
  });
  
  it('should not navigate to login on non-401 error', () => {
    mockLocalStorage.getItem.mockReturnValue('test-token');

    httpClient.get('/api/test').subscribe({
        error: () => {} // ignore error in test
    });

    const req = httpMock.expectOne('/api/test');
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });

    expect(mockLocalStorage.removeItem).not.toHaveBeenCalled();
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });
});
