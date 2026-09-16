import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../config/api.config';
import { Company, CreateCompanyRequest } from '../models/company.model';
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

  it('getAll sends a GET request to /api/companies and returns the typed response', () => {
    const mockCompanies: Company[] = [{ id: '1', name: 'Acme', websiteUrl: 'https://acme.com' }];
    let result: Company[] | undefined;

    service.getAll().subscribe((companies) => (result = companies));

    const req = httpMock.expectOne(companiesUrl);
    expect(req.request.method).toBe('GET');
    req.flush(mockCompanies);

    expect(result).toEqual(mockCompanies);
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
    const mockCompanies: Company[] = [{ id: '1', name: 'Acme', websiteUrl: 'https://acme.com' }];
    let result: Company[] | undefined;

    service.search('acme').subscribe((companies) => (result = companies));

    const req = httpMock.expectOne((request) => request.url === companiesUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('search')).toBe('acme');
    req.flush(mockCompanies);

    expect(result).toEqual(mockCompanies);
  });
});
