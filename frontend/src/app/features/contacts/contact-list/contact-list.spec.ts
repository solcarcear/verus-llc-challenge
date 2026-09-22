import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { ContactService } from '../../../core/services/contact.service';
import { Contact, PagedResult } from '../../../core/models/company.model';
import { ContactList } from './contact-list';

describe('ContactList', () => {
  let fixture: ComponentFixture<ContactList>;
  let getAllSpy: ReturnType<typeof vi.fn>;

  const contact: Contact = {
    id: '1',
    companyId: 'c1',
    firstName: 'Jane',
    lastName: 'Doe',
    email: 'jane@example.com',
    phone: null,
    jobTitle: 'Manager',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
  };

  function pageOf(items: Contact[]): PagedResult<Contact> {
    return { items, pageNumber: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
  }

  beforeEach(async () => {
    getAllSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [ContactList],
      providers: [{ provide: ContactService, useValue: { getAll: getAllSpy } }],
    }).compileComponents();

    fixture = TestBed.createComponent(ContactList);
  });

  it('loads contacts automatically on init', () => {
    getAllSpy.mockReturnValue(new Subject());

    fixture.detectChanges();

    expect(getAllSpy).toHaveBeenCalledWith(1, 20, { isActive: undefined });
  });

  it('displays a loading state while the request is pending', () => {
    getAllSpy.mockReturnValue(new Subject());

    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Loading contacts');
  });

  it('renders returned contacts', () => {
    const subject = new Subject<PagedResult<Contact>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next(pageOf([contact]));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Jane Doe');
    expect(text).toContain('jane@example.com');
  });

  it('shows an empty state when there are no results', () => {
    const subject = new Subject<PagedResult<Contact>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next(pageOf([]));
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No contacts found.');
  });

  it('shows a friendly error message when loading fails', () => {
    const subject = new Subject<PagedResult<Contact>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.error(new Error('boom'));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Unable to load contacts.');
    expect(text).not.toContain('boom');
  });

  it('re-queries with the isActive filter and resets to page 1 when the filter changes', () => {
    getAllSpy.mockReturnValue(new Subject());
    fixture.detectChanges();

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'active';
    select.dispatchEvent(new Event('change'));

    expect(getAllSpy).toHaveBeenLastCalledWith(1, 20, { isActive: true });
  });
});
