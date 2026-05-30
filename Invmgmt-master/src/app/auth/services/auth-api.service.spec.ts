import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthApiService, LoginPayload, RegisterPayload } from './auth-api.service';
import { environment } from '../../../environments/environment';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthApiService]
    });
    service = TestBed.inject(AuthApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should register', () => {
    const payload: RegisterPayload = {
      username: 'testuser',
      email: 'test@test.com',
      password: 'password',
      designation: 'dev',
      departmentId: 1,
      roleId: 2
    };

    service.register(payload).subscribe();
    
    const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ message: 'Registered' });
  });

  it('should login', () => {
    const payload: LoginPayload = {
      email: 'test@test.com',
      password: 'password'
    };

    service.login(payload).subscribe();
    
    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ token: 'abc', role: 'ADMIN', message: 'Logged in' });
  });
});
