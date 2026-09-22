import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../config/api.config';
import { Contact, PagedResult } from '../models/company.model';
import { ContactService } from './contact.service';

describe('ContactService', () => {
  let service: ContactService;
  let httpMock: HttpTestingController;
  const contactsUrl = `${API_BASE_URL}/contacts`;

  const mockPage: PagedResult<Contact> = {
    items: [],
    pageNumber: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ContactService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends pageNumber and pageSize with no filters by default', () => {
    service.getAll(2, 10).subscribe();

    const req = httpMock.expectOne((request) => request.url === contactsUrl);
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.has('companyId')).toBe(false);
    expect(req.request.params.has('isActive')).toBe(false);
    req.flush(mockPage);
  });

  it('includes companyId when provided', () => {
    service.getAll(1, 20, { companyId: 'company-1' }).subscribe();

    const req = httpMock.expectOne((request) => request.url === contactsUrl);
    expect(req.request.params.get('companyId')).toBe('company-1');
    req.flush(mockPage);
  });

  it('includes isActive when provided, including false', () => {
    service.getAll(1, 20, { isActive: false }).subscribe();

    const req = httpMock.expectOne((request) => request.url === contactsUrl);
    expect(req.request.params.get('isActive')).toBe('false');
    req.flush(mockPage);
  });
});
