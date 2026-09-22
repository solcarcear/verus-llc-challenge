import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DebugElement, Type } from '@angular/core';
import { By } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { App } from './app';
import { CompanyDetailsModal } from './features/companies/company-details-modal/company-details-modal';
import { CompanyEditModal } from './features/companies/company-edit-modal/company-edit-modal';
import { CompanyForm } from './features/companies/company-form/company-form';
import { CompanyList } from './features/companies/company-list/company-list';
import { CompanySearch } from './features/companies/company-search/company-search';
import { CompanyService } from './core/services/company.service';
import { Company, CompanyListItem, PagedResult } from './core/models/company.model';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let getAllSpy: ReturnType<typeof vi.fn>;
  let searchSpy: ReturnType<typeof vi.fn>;
  let updateSpy: ReturnType<typeof vi.fn>;
  let deleteSpy: ReturnType<typeof vi.fn>;
  let getDetailsSpy: ReturnType<typeof vi.fn>;

  const companies: CompanyListItem[] = [
    { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com', contactCount: 2, orderCount: 5 },
    { id: '2', name: 'Globex', websiteUrl: 'https://globex.com', contactCount: 0, orderCount: 0 },
  ];

  function pageOf(
    items: CompanyListItem[],
    overrides: Partial<Pick<PagedResult<CompanyListItem>, 'pageNumber' | 'totalPages'>> = {},
  ): PagedResult<CompanyListItem> {
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

  function clickTab(label: 'Companies' | 'Contacts' | 'Orders'): void {
    const buttons = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.view-tab'),
    );
    buttons.find((button) => button.textContent?.trim() === label)?.click();
    fixture.detectChanges();
  }

  beforeEach(async () => {
    getAllSpy = vi.fn();
    searchSpy = vi.fn();
    updateSpy = vi.fn();
    deleteSpy = vi.fn();
    getDetailsSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: CompanyService,
          useValue: {
            getAll: getAllSpy,
            search: searchSpy,
            create: vi.fn(),
            update: updateSpy,
            delete: deleteSpy,
            getDetails: getDetailsSpy,
          },
        },
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
    getAllSpy.mockReturnValue(new Subject<PagedResult<CompanyListItem>>());

    fixture.detectChanges();
    triggerLoad();

    expect(getAllSpy).toHaveBeenCalledTimes(1);
    expect(getAllSpy).toHaveBeenCalledWith(1, 20);
  });

  it('displays returned companies with their contact/order counts', () => {
    const subject = new Subject<PagedResult<CompanyListItem>>();
    getAllSpy.mockReturnValue(subject);

    fixture.detectChanges();
    triggerLoad();
    subject.next(pageOf(companies));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
    expect(text).toContain('Globex');
    expect(text).toContain('2 contacts');
    expect(text).toContain('5 orders');
  });

  it('displays a loading state while the request is pending', () => {
    getAllSpy.mockReturnValue(new Subject<PagedResult<CompanyListItem>>());

    fixture.detectChanges();
    triggerLoad();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Loading companies');
  });

  it('displays a friendly error state when loading fails', () => {
    const subject = new Subject<PagedResult<CompanyListItem>>();
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
    const initialLoad = new Subject<PagedResult<CompanyListItem>>();
    getAllSpy.mockReturnValue(initialLoad);

    fixture.detectChanges();
    triggerLoad();
    initialLoad.next(pageOf([companies[0]]));
    fixture.detectChanges();

    const reload = new Subject<PagedResult<CompanyListItem>>();
    getAllSpy.mockReturnValue(reload);

    const created: Company = { id: '3', name: 'Initech', websiteUrl: 'https://initech.com' };
    const formDebugElement = findComponent(CompanyForm);
    (formDebugElement.componentInstance as CompanyForm).created.emit(created);
    reload.next(pageOf(companies));
    fixture.detectChanges();

    expect(getAllSpy).toHaveBeenCalledTimes(2);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
    expect(text).toContain('Globex');
  });

  describe('view tabs', () => {
    it('shows the companies section by default', () => {
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('app-company-form')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('app-contact-list')).toBeFalsy();
      expect(fixture.nativeElement.querySelector('app-order-list')).toBeFalsy();
    });

    it('switches to the contacts view', () => {
      fixture.detectChanges();

      clickTab('Contacts');

      expect(fixture.nativeElement.querySelector('app-contact-list')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('app-company-form')).toBeFalsy();
    });

    it('switches to the orders view', () => {
      fixture.detectChanges();

      clickTab('Orders');

      expect(fixture.nativeElement.querySelector('app-order-list')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('app-company-form')).toBeFalsy();
    });
  });

  describe('search', () => {
    function triggerSearch(query: string): Subject<CompanyListItem[]> {
      const searchDebugElement = findComponent(CompanySearch);
      const subject = new Subject<CompanyListItem[]>();
      searchSpy.mockReturnValue(subject);
      (searchDebugElement.componentInstance as CompanySearch).searchRequested.emit(query);
      return subject;
    }

    function triggerClear(): void {
      const searchDebugElement = findComponent(CompanySearch);
      (searchDebugElement.componentInstance as CompanySearch).clearRequested.emit();
    }

    beforeEach(() => {
      const initialLoad = new Subject<PagedResult<CompanyListItem>>();
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

      const refetch = new Subject<PagedResult<CompanyListItem>>();
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

      const refetch = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(refetch);

      const created: Company = { id: '3', name: 'Initech', websiteUrl: 'https://initech.com' };
      const createdListItem: CompanyListItem = { ...created, contactCount: 0, orderCount: 0 };
      const formDebugElement = findComponent(CompanyForm);
      (formDebugElement.componentInstance as CompanyForm).created.emit(created);
      refetch.next(pageOf([...companies, createdListItem]));
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
      const subject = new Subject<PagedResult<CompanyListItem>>();
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

      const nextPage = new Subject<PagedResult<CompanyListItem>>();
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
      const nextPage = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(nextPage);
      findButton('Next')?.click();
      nextPage.next(pageOf(companies, { pageNumber: 2, totalPages: 3 }));
      fixture.detectChanges();

      const previousPage = new Subject<PagedResult<CompanyListItem>>();
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

      const searchSubject = new Subject<CompanyListItem[]>();
      searchSpy.mockReturnValue(searchSubject);
      const searchDebugElement = findComponent(CompanySearch);
      (searchDebugElement.componentInstance as CompanySearch).searchRequested.emit('acme');
      searchSubject.next([companies[0]]);
      fixture.detectChanges();

      expect((fixture.nativeElement as HTMLElement).querySelector('.pagination')).toBeNull();
    });
  });

  describe('details', () => {
    beforeEach(() => {
      const initialLoad = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(initialLoad);
      fixture.detectChanges();
      triggerLoad();
      initialLoad.next(pageOf(companies));
      fixture.detectChanges();
    });

    it('opens the details modal with the selected company', () => {
      getDetailsSpy.mockReturnValue(new Subject());
      const listDebugElement = findComponent(CompanyList);
      (listDebugElement.componentInstance as CompanyList).detailsRequested.emit(companies[0]);
      fixture.detectChanges();

      const modalDebugElement = findComponent(CompanyDetailsModal);
      expect(modalDebugElement).toBeTruthy();
      expect((modalDebugElement.componentInstance as CompanyDetailsModal).company()).toEqual(companies[0]);
    });

    it('closes the details modal without reloading the list', () => {
      getDetailsSpy.mockReturnValue(new Subject());
      const listDebugElement = findComponent(CompanyList);
      (listDebugElement.componentInstance as CompanyList).detailsRequested.emit(companies[0]);
      fixture.detectChanges();
      const callsBeforeClose = getAllSpy.mock.calls.length;

      const modalDebugElement = findComponent(CompanyDetailsModal);
      (modalDebugElement.componentInstance as CompanyDetailsModal).closed.emit();
      fixture.detectChanges();

      expect(findComponent(CompanyDetailsModal)).toBeFalsy();
      expect(getAllSpy).toHaveBeenCalledTimes(callsBeforeClose);
    });
  });

  describe('edit', () => {
    beforeEach(() => {
      const initialLoad = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(initialLoad);
      fixture.detectChanges();
      triggerLoad();
      initialLoad.next(pageOf(companies, { totalPages: 3 }));
      fixture.detectChanges();

      // Navigate to page 2 for real (clicking Next), since the component's
      // pageNumber signal is driven by user navigation, not by whatever
      // pageNumber a mocked response happens to report.
      const secondPage = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(secondPage);
      (fixture.nativeElement as HTMLElement)
        .querySelectorAll<HTMLButtonElement>('.pagination button')[1]
        ?.click();
      secondPage.next(pageOf(companies, { pageNumber: 2, totalPages: 3 }));
      fixture.detectChanges();
    });

    it('opens the modal with the selected company when Edit is requested', () => {
      const listDebugElement = findComponent(CompanyList);
      (listDebugElement.componentInstance as CompanyList).editRequested.emit(companies[0]);
      fixture.detectChanges();

      const modalDebugElement = findComponent(CompanyEditModal);
      expect(modalDebugElement).toBeTruthy();
      expect((modalDebugElement.componentInstance as CompanyEditModal).company()).toEqual(companies[0]);
    });

    it('closes the modal and reloads the same page when the edit is saved', () => {
      const listDebugElement = findComponent(CompanyList);
      (listDebugElement.componentInstance as CompanyList).editRequested.emit(companies[0]);
      fixture.detectChanges();

      const reload = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(reload);

      const modalDebugElement = findComponent(CompanyEditModal);
      const updated: Company = { id: '1', name: 'Acme Global', websiteUrl: 'https://acmeglobal.com' };
      (modalDebugElement.componentInstance as CompanyEditModal).saved.emit(updated);
      reload.next(pageOf(companies, { pageNumber: 2, totalPages: 3 }));
      fixture.detectChanges();

      expect(findComponent(CompanyEditModal)).toBeFalsy();
      // Still page 2 - saving an edit never changes which page is displayed.
      expect(getAllSpy).toHaveBeenLastCalledWith(2, 20);
    });

    it('closes the modal without reloading when the edit is cancelled', () => {
      const listDebugElement = findComponent(CompanyList);
      (listDebugElement.componentInstance as CompanyList).editRequested.emit(companies[0]);
      fixture.detectChanges();

      const callsBeforeCancel = getAllSpy.mock.calls.length;
      const modalDebugElement = findComponent(CompanyEditModal);
      (modalDebugElement.componentInstance as CompanyEditModal).cancelled.emit();
      fixture.detectChanges();

      expect(findComponent(CompanyEditModal)).toBeFalsy();
      expect(getAllSpy).toHaveBeenCalledTimes(callsBeforeCancel);
    });
  });

  describe('delete', () => {
    let confirmSpy: ReturnType<typeof vi.spyOn>;

    beforeEach(() => {
      confirmSpy = vi.spyOn(window, 'confirm');

      const initialLoad = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(initialLoad);
      fixture.detectChanges();
      triggerLoad();
      initialLoad.next(pageOf(companies, { totalPages: 2 }));
      fixture.detectChanges();
    });

    function requestDelete(company: CompanyListItem): void {
      const listDebugElement = findComponent(CompanyList);
      (listDebugElement.componentInstance as CompanyList).deleteRequested.emit(company);
    }

    it('does nothing if the user does not confirm the deletion', () => {
      confirmSpy.mockReturnValue(false);

      requestDelete(companies[0]);

      expect(deleteSpy).not.toHaveBeenCalled();
    });

    it('calls CompanyService.delete and shows a success message once the backend confirms', () => {
      confirmSpy.mockReturnValue(true);
      const deleteResult = new Subject<void>();
      deleteSpy.mockReturnValue(deleteResult);
      const reload = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(reload);

      requestDelete(companies[0]);
      expect(deleteSpy).toHaveBeenCalledWith('1');

      deleteResult.next();
      reload.next(pageOf([companies[1]]));
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Company deleted successfully.');
    });

    it('shows an error message and does not reload if deletion fails', () => {
      confirmSpy.mockReturnValue(true);
      const deleteResult = new Subject<void>();
      deleteSpy.mockReturnValue(deleteResult);
      const callsBeforeDelete = getAllSpy.mock.calls.length;

      requestDelete(companies[0]);
      deleteResult.error(new Error('boom'));
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Unable to delete the company.');
      expect(getAllSpy).toHaveBeenCalledTimes(callsBeforeDelete);
    });

    it('steps back to the previous page when deleting leaves the current page empty', () => {
      confirmSpy.mockReturnValue(true);
      const deleteResult = new Subject<void>();
      deleteSpy.mockReturnValue(deleteResult);

      // Move to page 2 first.
      const secondPage = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValue(secondPage);
      (fixture.nativeElement as HTMLElement)
        .querySelectorAll<HTMLButtonElement>('.pagination button')[1]
        ?.click();
      secondPage.next(pageOf([companies[0]], { pageNumber: 2, totalPages: 2 }));
      fixture.detectChanges();

      const afterDelete = new Subject<PagedResult<CompanyListItem>>();
      const previousPage = new Subject<PagedResult<CompanyListItem>>();
      getAllSpy.mockReturnValueOnce(afterDelete).mockReturnValueOnce(previousPage);

      requestDelete(companies[0]);
      deleteResult.next();
      // Page 2 comes back empty now that its only company was deleted.
      afterDelete.next({ items: [], pageNumber: 2, pageSize: 20, totalCount: 1, totalPages: 1 });
      previousPage.next(pageOf([companies[1]], { pageNumber: 1, totalPages: 1 }));
      fixture.detectChanges();

      expect(getAllSpy).toHaveBeenLastCalledWith(1, 20);
      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Page 1 of 1');
    });
  });
});
