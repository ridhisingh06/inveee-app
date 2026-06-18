import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PersonnelService } from './personnel.service';
import { environment } from '../../environments/environment';

describe('PersonnelService', () => {
  let service: PersonnelService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PersonnelService]
    });
    service = TestBed.inject(PersonnelService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get personnel', () => {
    service.getPersonnel(2, 50).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/personnel?page=2&pageSize=50`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('should delete personnel', () => {
    service.deletePersonnel(1).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/personnel/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ message: 'Deleted' });
  });
});
