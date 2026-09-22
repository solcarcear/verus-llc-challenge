import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../config/api.config';
import { Order, PagedResult } from '../models/company.model';
import { OrderService } from './order.service';

describe('OrderService', () => {
  let service: OrderService;
  let httpMock: HttpTestingController;
  const ordersUrl = `${API_BASE_URL}/orders`;

  const mockPage: PagedResult<Order> = {
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

    service = TestBed.inject(OrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends pageNumber and pageSize with no filters by default', () => {
    service.getAll(1, 20).subscribe();

    const req = httpMock.expectOne((request) => request.url === ordersUrl);
    expect(req.request.params.has('companyId')).toBe(false);
    expect(req.request.params.has('status')).toBe(false);
    req.flush(mockPage);
  });

  it('includes companyId when provided', () => {
    service.getAll(1, 20, { companyId: 'company-1' }).subscribe();

    const req = httpMock.expectOne((request) => request.url === ordersUrl);
    expect(req.request.params.get('companyId')).toBe('company-1');
    req.flush(mockPage);
  });

  it('includes status when provided', () => {
    service.getAll(1, 20, { status: 'Completed' }).subscribe();

    const req = httpMock.expectOne((request) => request.url === ordersUrl);
    expect(req.request.params.get('status')).toBe('Completed');
    req.flush(mockPage);
  });
});
