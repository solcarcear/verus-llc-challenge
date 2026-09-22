import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../config/api.config';
import {
  Company,
  CompanyDetails,
  CompanyListItem,
  CreateCompanyRequest,
  PagedResult,
  UpdateCompanyRequest,
} from '../models/company.model';
import { CompanyService } from './company.service';

describe('CompanyService', () => {
  let service: CompanyService;
  let httpMock: HttpTestingController;
  const companiesUrl = `${API_BASE_URL}/companies`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CompanyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAll sends a GET request with pageNumber/pageSize and returns the typed page', () => {
    const mockPage: PagedResult<CompanyListItem> = {
      items: [{ id: '1', name: 'Acme', websiteUrl: 'https://acme.com', contactCount: 3, orderCount: 7 }],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    };
    let result: PagedResult<CompanyListItem> | undefined;

    service.getAll(1, 20).subscribe((page) => (result = page));

    const req = httpMock.expectOne((request) => request.url === companiesUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush(mockPage);

    expect(result).toEqual(mockPage);
  });

  it('getById sends a GET request to /api/companies/{id}', () => {
    const mockCompany: Company = { id: '1', name: 'Acme', websiteUrl: 'https://acme.com' };
    let result: Company | undefined;

    service.getById('1').subscribe((company) => (result = company));

    const req = httpMock.expectOne(`${companiesUrl}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockCompany);

    expect(result).toEqual(mockCompany);
  });

  it('getDetails sends a GET request to /api/companies/{id}/details', () => {
    const mockDetails: CompanyDetails = {
      id: '1',
      name: 'Acme',
      websiteUrl: 'https://acme.com',
      contacts: [],
      orders: [],
    };
    let result: CompanyDetails | undefined;

    service.getDetails('1').subscribe((details) => (result = details));

    const req = httpMock.expectOne(`${companiesUrl}/1/details`);
    expect(req.request.method).toBe('GET');
    req.flush(mockDetails);

    expect(result).toEqual(mockDetails);
  });

  it('create sends a POST request to /api/companies with the expected body', () => {
    const request: CreateCompanyRequest = { name: 'Acme', websiteUrl: 'https://acme.com' };
    const mockResponse: Company = { id: '1', ...request };
    let result: Company | undefined;

    service.create(request).subscribe((company) => (result = company));

    const req = httpMock.expectOne(companiesUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });

  it('search sends a GET request with the search query parameter', () => {
    const mockCompanies: CompanyListItem[] = [
      { id: '1', name: 'Acme', websiteUrl: 'https://acme.com', contactCount: 1, orderCount: 2 },
    ];
    let result: CompanyListItem[] | undefined;

    service.search('acme').subscribe((companies) => (result = companies));

    const req = httpMock.expectOne((request) => request.url === companiesUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('search')).toBe('acme');
    req.flush(mockCompanies);

    expect(result).toEqual(mockCompanies);
  });

  it('update sends a PUT request to /api/companies/{id} with the expected body', () => {
    const request: UpdateCompanyRequest = { name: 'Acme Global', websiteUrl: 'https://acmeglobal.com' };
    const mockResponse: Company = { id: '1', ...request };
    let result: Company | undefined;

    service.update('1', request).subscribe((company) => (result = company));

    const req = httpMock.expectOne(`${companiesUrl}/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });

  it('delete sends a DELETE request to /api/companies/{id}', () => {
    let completed = false;

    service.delete('1').subscribe(() => (completed = true));

    const req = httpMock.expectOne(`${companiesUrl}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(completed).toBe(true);
  });
});
