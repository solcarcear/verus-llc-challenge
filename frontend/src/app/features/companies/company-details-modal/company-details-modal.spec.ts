import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { CompanyService } from '../../../core/services/company.service';
import { CompanyDetails, CompanyListItem } from '../../../core/models/company.model';
import { CompanyDetailsModal } from './company-details-modal';

describe('CompanyDetailsModal', () => {
  let fixture: ComponentFixture<CompanyDetailsModal>;
  let getDetailsSpy: ReturnType<typeof vi.fn>;

  const company: CompanyListItem = {
    id: '1',
    name: 'Acme Corp',
    websiteUrl: 'https://acme.com',
    contactCount: 1,
    orderCount: 1,
  };

  const details: CompanyDetails = {
    id: '1',
    name: 'Acme Corp',
    websiteUrl: 'https://acme.com',
    contacts: [
      {
        id: 'c1',
        companyId: '1',
        firstName: 'Jane',
        lastName: 'Doe',
        email: 'jane@example.com',
        phone: null,
        jobTitle: 'Manager',
        isActive: true,
        createdAt: '2026-01-01T00:00:00Z',
      },
    ],
    orders: [
      {
        id: 'o1',
        companyId: '1',
        orderNumber: 'ORD-001',
        amount: 250,
        status: 'Completed',
        createdAt: '2026-01-02T00:00:00Z',
        updatedAt: null,
      },
    ],
  };

  beforeEach(async () => {
    getDetailsSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [CompanyDetailsModal],
      providers: [{ provide: CompanyService, useValue: { getDetails: getDetailsSpy } }],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanyDetailsModal);
    fixture.componentRef.setInput('company', company);
  });

  it('fetches details for the given company on init', () => {
    getDetailsSpy.mockReturnValue(new Subject());

    fixture.detectChanges();

    expect(getDetailsSpy).toHaveBeenCalledWith('1');
  });

  it('shows a loading state while the request is pending', () => {
    getDetailsSpy.mockReturnValue(new Subject());

    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Loading company details');
  });

  it('renders contacts and orders once loaded', () => {
    const subject = new Subject<CompanyDetails>();
    getDetailsSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next(details);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Jane Doe');
    expect(text).toContain('ORD-001');
  });

  it('shows a friendly error message when loading fails', () => {
    const subject = new Subject<CompanyDetails>();
    getDetailsSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.error(new Error('boom'));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Unable to load company details.');
    expect(text).not.toContain('boom');
  });

  it('emits closed when Close is clicked', () => {
    getDetailsSpy.mockReturnValue(new Subject());
    fixture.detectChanges();
    let closed = false;
    fixture.componentInstance.closed.subscribe(() => (closed = true));

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button.secondary')?.click();

    expect(closed).toBe(true);
  });
});
