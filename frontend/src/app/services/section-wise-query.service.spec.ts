import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SectionWiseQueryService } from './section-wise-query.service';
import { environment } from '../../environments/environment';

describe('SectionWiseQueryService', () => {
  let service: SectionWiseQueryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SectionWiseQueryService]
    });
    service = TestBed.inject(SectionWiseQueryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get officers', () => {
    service.getOfficers().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/admin/section-wise-query/officers`);
    expect(req.request.method).toBe('GET');
    req.flush({ officers: [] });
  });

  it('should get bhawans', () => {
    service.getBhawans().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/admin/section-wise-query/bhawans`);
    expect(req.request.method).toBe('GET');
    req.flush({ bhawans: [] });
  });

  it('should search items', () => {
    service.searchItems('pen').subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/admin/section-wise-query/items/search?query=pen`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });

  it('should get section wise query with all filters', () => {
    service.getSectionWiseQuery({
      officerId: 1,
      fromDate: '2023-01-01',
      toDate: '2023-12-31',
      bhawan: 'A',
      itemCode: '2',
      itemName: 'Pencil',
      pageNumber: 2,
      pageSize: 50
    }).subscribe();

    const req = httpMock.expectOne(request => 
        request.url === `${environment.apiUrl}/admin/section-wise-query` &&
        request.params.get('officerId') === '1' &&
        request.params.get('fromDate') === '2023-01-01' &&
        request.params.get('toDate') === '2023-12-31' &&
        request.params.get('bhawan') === 'A' &&
        request.params.get('itemCode') === '2' &&
        request.params.get('itemName') === 'Pencil' &&
        request.params.get('pageNumber') === '2' &&
        request.params.get('pageSize') === '50'
    );
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('should get section wise query with defaults', () => {
    service.getSectionWiseQuery({}).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/admin/section-wise-query?pageNumber=1&pageSize=20`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('should export csv', () => {
    service.exportCsv({ officerId: 1 }).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/admin/section-wise-query/export?officerId=1&pageNumber=1&pageSize=10000`);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob());
  });
});
