import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CompanyDetailsModal } from './features/companies/company-details-modal/company-details-modal';
import { CompanyEditModal } from './features/companies/company-edit-modal/company-edit-modal';
import { CompanyForm } from './features/companies/company-form/company-form';
import { CompanyList } from './features/companies/company-list/company-list';
import { CompanySearch } from './features/companies/company-search/company-search';
import { ContactList } from './features/contacts/contact-list/contact-list';
import { OrderList } from './features/orders/order-list/order-list';
import { CompanyService } from './core/services/company.service';
import { CompanyListItem } from './core/models/company.model';

type View = 'companies' | 'contacts' | 'orders';

@Component({
  imports: [
    RouterOutlet,
    CompanyDetailsModal,
    CompanyEditModal,
    CompanyForm,
    CompanyList,
    CompanySearch,
    ContactList,
    OrderList,
  ],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly companyService = inject(CompanyService);
  private readonly pageSize = 20;
  private inFlightSearchQuery: string | null = null;

  // Simple signal-driven tab switch instead of Angular Router: the app has no
  // deep-linkable routes today (app.routes.ts is empty), and one flag is enough
  // to show one of three top-level sections - adding real routing for that
  // would be more machinery than this screen needs.
  protected readonly activeView = signal<View>('companies');

  protected readonly companies = signal<readonly CompanyListItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly searching = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly deleteMessage = signal<string | null>(null);
  protected readonly activeSearchQuery = signal<string | null>(null);
  protected readonly hasFetched = signal(false);
  protected readonly pageNumber = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly editingCompany = signal<CompanyListItem | null>(null);
  protected readonly viewingCompany = signal<CompanyListItem | null>(null);

  protected readonly emptyMessage = computed(() => {
    const query = this.activeSearchQuery();
    return query ? `No companies found for "${query}".` : 'No companies have been added yet.';
  });

  protected onViewChange(view: View): void {
    this.activeView.set(view);
  }

  protected onCompanyCreated(_company: unknown): void {
    this.activeSearchQuery.set(null);
    this.deleteMessage.set(null);
    // Re-fetch rather than append locally: the current page is alphabetically
    // ordered, so a locally-appended company would rarely belong at the end of
    // it, and totalCount/totalPages would otherwise go stale.
    this.loadCompanies();
  }

  protected onDetailsRequested(company: CompanyListItem): void {
    this.viewingCompany.set(company);
  }

  protected onDetailsClosed(): void {
    this.viewingCompany.set(null);
  }

  protected onEditRequested(company: CompanyListItem): void {
    this.deleteMessage.set(null);
    this.editingCompany.set(company);
  }

  protected onEditCancelled(): void {
    this.editingCompany.set(null);
  }

  protected onCompanyUpdated(_updated: unknown): void {
    this.editingCompany.set(null);
    // pageNumber is untouched, so this simply reloads the page the user was
    // already looking at - editing never jumps them anywhere else.
    this.loadCompanies();
  }

  protected onDeleteRequested(company: CompanyListItem): void {
    const confirmed = window.confirm(`Delete "${company.name}"? This cannot be undone.`);
    if (!confirmed) {
      return;
    }

    this.errorMessage.set(null);
    this.deleteMessage.set(null);

    this.companyService.delete(company.id).subscribe({
      next: () => {
        this.deleteMessage.set('Company deleted successfully.');
        this.loadCompanies();
      },
      error: () => {
        this.errorMessage.set('Unable to delete the company.');
      },
    });
  }

  protected onSearchRequested(query: string): void {
    if (this.searching() && this.inFlightSearchQuery === query) {
      return;
    }

    this.inFlightSearchQuery = query;
    this.searching.set(true);
    this.errorMessage.set(null);
    this.deleteMessage.set(null);

    this.companyService.search(query).subscribe({
      next: (results) => {
        this.companies.set(results);
        this.activeSearchQuery.set(query);
        this.hasFetched.set(true);
        this.searching.set(false);
        this.inFlightSearchQuery = null;
      },
      error: () => {
        this.errorMessage.set('Search failed. Please try again later.');
        this.searching.set(false);
        this.inFlightSearchQuery = null;
      },
    });
  }

  protected onClearRequested(): void {
    this.activeSearchQuery.set(null);
    this.errorMessage.set(null);
    this.pageNumber.set(1);
    this.loadCompanies();
  }

  protected onPreviousPage(): void {
    if (this.pageNumber() <= 1) {
      return;
    }

    this.pageNumber.update((current) => current - 1);
    this.loadCompanies();
  }

  protected onNextPage(): void {
    if (this.pageNumber() >= this.totalPages()) {
      return;
    }

    this.pageNumber.update((current) => current + 1);
    this.loadCompanies();
  }

  protected loadCompanies(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.companyService.getAll(this.pageNumber(), this.pageSize).subscribe({
      next: (page) => {
        if (page.items.length === 0 && page.pageNumber > 1) {
          // We asked for a page past the last one - most likely because the
          // record we just deleted was the only one left on it. Step back one
          // page and re-fetch instead of showing an empty page to the user.
          this.pageNumber.set(page.pageNumber - 1);
          this.loadCompanies();
          return;
        }

        this.companies.set(page.items);
        this.totalPages.set(page.totalPages);
        this.hasFetched.set(true);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Companies could not be loaded. Please try again later.');
        this.loading.set(false);
      },
    });
  }
}
