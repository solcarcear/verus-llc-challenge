import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { OrderService } from '../../../core/services/order.service';
import { Order, PagedResult } from '../../../core/models/company.model';
import { OrderList } from './order-list';

describe('OrderList', () => {
  let fixture: ComponentFixture<OrderList>;
  let getAllSpy: ReturnType<typeof vi.fn>;

  const order: Order = {
    id: '1',
    companyId: 'c1',
    orderNumber: 'ORD-001',
    amount: 199.5,
    status: 'Completed',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
  };

  function pageOf(items: Order[]): PagedResult<Order> {
    return { items, pageNumber: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
  }

  beforeEach(async () => {
    getAllSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [OrderList],
      providers: [{ provide: OrderService, useValue: { getAll: getAllSpy } }],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderList);
  });

  it('loads orders automatically on init', () => {
    getAllSpy.mockReturnValue(new Subject());

    fixture.detectChanges();

    expect(getAllSpy).toHaveBeenCalledWith(1, 20, { status: undefined });
  });

  it('renders returned orders', () => {
    const subject = new Subject<PagedResult<Order>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next(pageOf([order]));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('ORD-001');
    expect(text).toContain('Completed');
  });

  it('shows an empty state when there are no results', () => {
    const subject = new Subject<PagedResult<Order>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next(pageOf([]));
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No orders found.');
  });

  it('shows a friendly error message when loading fails', () => {
    const subject = new Subject<PagedResult<Order>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.error(new Error('boom'));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Unable to load orders.');
    expect(text).not.toContain('boom');
  });

  it('re-queries with the status filter and resets to page 1 when the filter changes', () => {
    getAllSpy.mockReturnValue(new Subject());
    fixture.detectChanges();

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'Pending';
    select.dispatchEvent(new Event('change'));

    expect(getAllSpy).toHaveBeenLastCalledWith(1, 20, { status: 'Pending' });
  });
});
