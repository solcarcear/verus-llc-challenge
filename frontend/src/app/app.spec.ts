import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DebugElement, Type } from '@angular/core';
import { By } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { App } from './app';
import { CompanyForm } from './features/companies/company-form/company-form';
import { CompanySearch } from './features/companies/company-search/company-search';
import { CompanyService } from './core/services/company.service';
import { Company, PagedResult } from './core/models/company.model';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let getAllSpy: ReturnType<typeof vi.fn>;
  let searchSpy: ReturnType<typeof vi.fn>;

  const companies: Company[] = [
    { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com' },
    { id: '2', name: 'Globex', websiteUrl: 'https://globex.com' },
  ];

  function pageOf(
    items: Company[],
    overrides: Partial<Pick<PagedResult<Company>, 'pageNumber' | 'totalPages'>> = {},
  ): PagedResult<Company> {
    return {
      items,
      pageNumber: overrides.pageNumber ?? 1,
      pageSize: 20,
      totalCount: items.length,
      totalPages: overrides.totalPages ?? 1,
    };
  }

  function findComponent<T>(type: Type<T>): DebugElement {
    return fixture.debugElement.query(By.directive(type));
  }

  function triggerLoad(): void {
    const loadButton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.initial-state button',
    );
    loadButton?.click();
    fixture.detectChanges();
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

  it('does not call CompanyService.getAll automatically on initialization', () => {
    fixture.detectChanges();

    expect(getAllSpy).not.toHaveBeenCalled();
  });

  it('prompts the user to load companies before any data has been fetched', () => {
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Company list not loaded yet.');
  });

  it('calls CompanyService.getAll with the default page number and size when the user requests to load companies', () => {
    getAllSpy.mockReturnValue(new Subject<PagedResult<Company>>());

    fixture.detectChanges();
    triggerLoad();

    expect(getAllSpy).toHaveBeenCalledTimes(1);
    expect(getAllSpy).toHaveBeenCalledWith(1, 20);
  });

  it('displays returned companies', () => {
    const subject = new Subject<PagedResult<Company>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    triggerLoad();
    subject.next(pageOf(companies));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
    expect(text).toContain('Globex');
  });

  it('displays a loading state while the request is pending', () => {
    getAllSpy.mockReturnValue(new Subject<PagedResult<Company>>());

    fixture.detectChanges();
    triggerLoad();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Loading companies');
  });

  it('displays a friendly error state when loading fails', () => {
    const subject = new Subject<PagedResult<Company>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    triggerLoad();
    subject.error(new Error('network down'));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Companies could not be loaded. Please try again later.');
    expect(text).not.toContain('network down');
  });

  it('reloads the current page after a company is created, instead of appending it locally', () => {
    const initialLoad = new Subject<PagedResult<Company>>();
    getAllSpy.mockReturnValue(initialLoad);

    fixture.detectChanges();
    triggerLoad();
    initialLoad.next(pageOf([companies[0]]));
    fixture.detectChanges();

    const reload = new Subject<PagedResult<Company>>();
    getAllSpy.mockReturnValue(reload);

    const formDebugElement = findComponent(CompanyForm);
    (formDebugElement.componentInstance as CompanyForm).created.emit(companies[1]);
    reload.next(pageOf(companies));
    fixture.detectChanges();

    expect(getAllSpy).toHaveBeenCalledTimes(2);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
    expect(text).toContain('Globex');
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
      const initialLoad = new Subject<PagedResult<Company>>();
      getAllSpy.mockReturnValue(initialLoad);
      fixture.detectChanges();
      triggerLoad();
      initialLoad.next(pageOf(companies));
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

      const refetch = new Subject<PagedResult<Company>>();
      getAllSpy.mockReturnValue(refetch);
      triggerClear();
      refetch.next(pageOf(companies));
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

      const refetch = new Subject<PagedResult<Company>>();
      getAllSpy.mockReturnValue(refetch);

      const created: Company = { id: '3', name: 'Initech', websiteUrl: 'https://initech.com' };
      const formDebugElement = findComponent(CompanyForm);
      (formDebugElement.componentInstance as CompanyForm).created.emit(created);
      refetch.next(pageOf([...companies, created]));
      fixture.detectChanges();

      expect(getAllSpy).toHaveBeenCalledTimes(2);
      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Acme Corp');
      expect(text).toContain('Globex');
      expect(text).toContain('Initech');
    });
  });

  describe('pagination', () => {
    function loadFirstPage(totalPages: number): void {
      const subject = new Subject<PagedResult<Company>>();
      getAllSpy.mockReturnValue(subject);
      fixture.detectChanges();
      triggerLoad();
      subject.next(pageOf(companies, { pageNumber: 1, totalPages }));
      fixture.detectChanges();
    }

    function findButton(label: string): HTMLButtonElement | null {
      const buttons = Array.from(
        (fixture.nativeElement as HTMLElement).querySelectorAll('.pagination button'),
      ) as HTMLButtonElement[];
      return buttons.find((button) => button.textContent?.trim() === label) ?? null;
    }

    it('shows the current page and total pages', () => {
      loadFirstPage(3);

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Page 1 of 3');
    });

    it('disables Previous on the first page', () => {
      loadFirstPage(3);

      expect(findButton('Previous')?.disabled).toBe(true);
    });

    it('disables Next on the last page', () => {
      loadFirstPage(1);

      expect(findButton('Next')?.disabled).toBe(true);
    });

    it('requests the next page when Next is clicked', () => {
      loadFirstPage(3);

      const nextPage = new Subject<PagedResult<Company>>();
      getAllSpy.mockReturnValue(nextPage);
      findButton('Next')?.click();
      nextPage.next(pageOf(companies, { pageNumber: 2, totalPages: 3 }));
      fixture.detectChanges();

      expect(getAllSpy).toHaveBeenLastCalledWith(2, 20);
      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Page 2 of 3');
    });

    it('requests the previous page when Previous is clicked', () => {
      loadFirstPage(3);
      const nextPage = new Subject<PagedResult<Company>>();
      getAllSpy.mockReturnValue(nextPage);
      findButton('Next')?.click();
      nextPage.next(pageOf(companies, { pageNumber: 2, totalPages: 3 }));
      fixture.detectChanges();

      const previousPage = new Subject<PagedResult<Company>>();
      getAllSpy.mockReturnValue(previousPage);
      findButton('Previous')?.click();
      previousPage.next(pageOf(companies, { pageNumber: 1, totalPages: 3 }));
      fixture.detectChanges();

      expect(getAllSpy).toHaveBeenLastCalledWith(1, 20);
      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Page 1 of 3');
    });

    it('does not show pagination controls while a search is active', () => {
      loadFirstPage(3);

      const searchSubject = new Subject<Company[]>();
      searchSpy.mockReturnValue(searchSubject);
      const searchDebugElement = findComponent(CompanySearch);
      (searchDebugElement.componentInstance as CompanySearch).searchRequested.emit('acme');
      searchSubject.next([companies[0]]);
      fixture.detectChanges();

      expect((fixture.nativeElement as HTMLElement).querySelector('.pagination')).toBeNull();
    });
  });
});
