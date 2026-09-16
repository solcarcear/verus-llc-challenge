import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DebugElement } from '@angular/core';
import { Subject } from 'rxjs';
import { App } from './app';
import { CompanyForm } from './features/companies/company-form/company-form';
import { CompanySearch } from './features/companies/company-search/company-search';
import { CompanyService } from './core/services/company.service';
import { Company } from './core/models/company.model';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let getAllSpy: ReturnType<typeof vi.fn>;
  let searchSpy: ReturnType<typeof vi.fn>;

  const companies: Company[] = [
    { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com' },
    { id: '2', name: 'Globex', websiteUrl: 'https://globex.com' },
  ];

  function findComponent<T>(type: new (...args: never[]) => T): DebugElement {
    return fixture.debugElement.children.find(
      (child) => child.componentInstance instanceof type,
    )!;
  }

  beforeEach(async () => {
    getAllSpy = vi.fn();
    searchSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        { provide: CompanyService, useValue: { getAll: getAllSpy, search: searchSpy, create: vi.fn() } },
      ],
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

  it('adds a newly created company to the current collection when no search is active', () => {
    const subject = new Subject<Company[]>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    subject.next([companies[0]]);
    fixture.detectChanges();

    const formDebugElement = findComponent(CompanyForm);
    (formDebugElement.componentInstance as CompanyForm).created.emit(companies[1]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
    expect(text).toContain('Globex');
    expect(getAllSpy).toHaveBeenCalledTimes(1);
  });

  describe('search', () => {
    function triggerSearch(query: string): Subject<Company[]> {
      const searchDebugElement = findComponent(CompanySearch);
      const subject = new Subject<Company[]>();
      searchSpy.mockReturnValue(subject);
      (searchDebugElement.componentInstance as CompanySearch).searchRequested.emit(query);
      return subject;
    }

    function triggerClear(): void {
      const searchDebugElement = findComponent(CompanySearch);
      (searchDebugElement.componentInstance as CompanySearch).clearRequested.emit();
    }

    beforeEach(() => {
      const initialLoad = new Subject<Company[]>();
      getAllSpy.mockReturnValue(initialLoad);
      fixture.detectChanges();
      initialLoad.next(companies);
      fixture.detectChanges();
    });

    it('calls CompanyService.search with the query on searchRequested', () => {
      triggerSearch('acme');

      expect(searchSpy).toHaveBeenCalledWith('acme');
    });

    it('replaces the displayed companies with search results without client-side filtering', () => {
      const subject = triggerSearch('acme');
      subject.next([companies[0]]);
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Acme Corp');
      expect(text).not.toContain('Globex');
    });

    it('shows the search-specific empty state when results are empty', () => {
      const subject = triggerSearch('micro');
      subject.next([]);
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('No companies found for "micro".');
    });

    it('displays a friendly message when search fails', () => {
      const subject = triggerSearch('acme');
      subject.error(new Error('boom'));
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Search failed. Please try again later.');
      expect(text).not.toContain('boom');
    });

    it('restores the full collection via getAll when clear is requested', () => {
      const searchSubject = triggerSearch('acme');
      searchSubject.next([companies[0]]);
      fixture.detectChanges();

      const refetch = new Subject<Company[]>();
      getAllSpy.mockReturnValue(refetch);
      triggerClear();
      refetch.next(companies);
      fixture.detectChanges();

      expect(getAllSpy).toHaveBeenCalledTimes(2);
      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Acme Corp');
      expect(text).toContain('Globex');
    });

    it('clears the active search and reloads all companies when a company is created during a search', () => {
      const searchSubject = triggerSearch('acme');
      searchSubject.next([companies[0]]);
      fixture.detectChanges();

      const refetch = new Subject<Company[]>();
      getAllSpy.mockReturnValue(refetch);

      const created: Company = { id: '3', name: 'Initech', websiteUrl: 'https://initech.com' };
      const formDebugElement = findComponent(CompanyForm);
      (formDebugElement.componentInstance as CompanyForm).created.emit(created);
      refetch.next([...companies, created]);
      fixture.detectChanges();

      expect(getAllSpy).toHaveBeenCalledTimes(2);
      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Acme Corp');
      expect(text).toContain('Globex');
      expect(text).toContain('Initech');
    });
  });
});
