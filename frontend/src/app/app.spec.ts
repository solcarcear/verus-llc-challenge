import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { App } from './app';
import { CompanyForm } from './features/companies/company-form/company-form';
import { CompanyService } from './core/services/company.service';
import { Company } from './core/models/company.model';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let getAllSpy: ReturnType<typeof vi.fn>;

  const companies: Company[] = [
    { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com' },
    { id: '2', name: 'Globex', websiteUrl: 'https://globex.com' },
  ];

  beforeEach(async () => {
    getAllSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: CompanyService, useValue: { getAll: getAllSpy, create: vi.fn() } }],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
  });

  it('calls CompanyService.getAll on initialization', () => {
    getAllSpy.mockReturnValue(new Subject<Company[]>());

    fixture.detectChanges();

    expect(getAllSpy).toHaveBeenCalledTimes(1);
  });

  it('displays returned companies', () => {
    const subject = new Subject<Company[]>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next(companies);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
    expect(text).toContain('Globex');
  });

  it('displays a loading state while the request is pending', () => {
    getAllSpy.mockReturnValue(new Subject<Company[]>());

    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Loading companies');
  });

  it('displays a friendly error state when loading fails', () => {
    const subject = new Subject<Company[]>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.error(new Error('network down'));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Companies could not be loaded. Please try again later.');
    expect(text).not.toContain('network down');
  });

  it('adds a newly created company to the current collection when CompanyForm emits created', () => {
    const subject = new Subject<Company[]>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next([companies[0]]);
    fixture.detectChanges();

    const formDebugElement = fixture.debugElement.children.find(
      (child) => child.componentInstance instanceof CompanyForm,
    )!;
    (formDebugElement.componentInstance as CompanyForm).created.emit(companies[1]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
    expect(text).toContain('Globex');
  });

  it('does not perform another GET after a company is created', () => {
    const subject = new Subject<Company[]>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next([companies[0]]);
    fixture.detectChanges();

    const formDebugElement = fixture.debugElement.children.find(
      (child) => child.componentInstance instanceof CompanyForm,
    )!;
    (formDebugElement.componentInstance as CompanyForm).created.emit(companies[1]);
    fixture.detectChanges();

    expect(getAllSpy).toHaveBeenCalledTimes(1);
  });
});
