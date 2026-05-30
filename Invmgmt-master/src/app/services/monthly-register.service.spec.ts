import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { MonthlyRegisterService } from './monthly-register.service';
import { environment } from '../../environments/environment';

describe('MonthlyRegisterService', () => {
  let service: MonthlyRegisterService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [MonthlyRegisterService]
    });
    service = TestBed.inject(MonthlyRegisterService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get monthly register without search', () => {
    service.getMonthlyRegister(5, 2023, 2, 50).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/admin/monthly-register?month=5&year=2023&pageNumber=2&pageSize=50`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('should get monthly register with search', () => {
    service.getMonthlyRegister(5, 2023, 1, 20, 'test').subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/admin/monthly-register?month=5&year=2023&pageNumber=1&pageSize=20&search=test`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
  
  it('should get monthly register ignoring empty search', () => {
    service.getMonthlyRegister(5, 2023, 1, 20, '   ').subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/admin/monthly-register?month=5&year=2023&pageNumber=1&pageSize=20`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
