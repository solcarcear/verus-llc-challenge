import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CompanyEditModal } from './features/companies/company-edit-modal/company-edit-modal';
import { CompanyForm } from './features/companies/company-form/company-form';
import { CompanyList } from './features/companies/company-list/company-list';
import { CompanySearch } from './features/companies/company-search/company-search';
import { CompanyService } from './core/services/company.service';
import { Company } from './core/models/company.model';

@Component({
  imports: [RouterOutlet, CompanyEditModal, CompanyForm, CompanyList, CompanySearch],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly companyService = inject(CompanyService);
  private readonly pageSize = 20;
  private inFlightSearchQuery: string | null = null;

  protected readonly companies = signal<readonly Company[]>([]);
  protected readonly loading = signal(false);
  protected readonly searching = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly deleteMessage = signal<string | null>(null);
  protected readonly activeSearchQuery = signal<string | null>(null);
  protected readonly hasFetched = signal(false);
  protected readonly pageNumber = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly editingCompany = signal<Company | null>(null);

  protected readonly emptyMessage = computed(() => {
    const query = this.activeSearchQuery();
    return query ? `No companies found for "${query}".` : 'No companies have been added yet.';
  });

  protected onCompanyCreated(_company: Company): void {
    this.activeSearchQuery.set(null);
    this.deleteMessage.set(null);
    // Re-fetch rather than append locally: the current page is alphabetically
    // ordered, so a locally-appended company would rarely belong at the end of
    // it, and totalCount/totalPages would otherwise go stale.
    this.loadCompanies();
  }

  protected onEditRequested(company: Company): void {
    this.deleteMessage.set(null);
    this.editingCompany.set(company);
  }

  protected onEditCancelled(): void {
    this.editingCompany.set(null);
  }

  protected onCompanyUpdated(_updated: Company): void {
    this.editingCompany.set(null);
    // pageNumber is untouched, so this simply reloads the page the user was
    // already looking at - editing never jumps them anywhere else.
    this.loadCompanies();
  }

  protected onDeleteRequested(company: Company): void {
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
