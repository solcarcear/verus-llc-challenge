import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CompanyForm } from './features/companies/company-form/company-form';
import { CompanyList } from './features/companies/company-list/company-list';
import { CompanySearch } from './features/companies/company-search/company-search';
import { CompanyService } from './core/services/company.service';
import { Company } from './core/models/company.model';

@Component({
  imports: [RouterOutlet, CompanyForm, CompanyList, CompanySearch],
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
  protected readonly activeSearchQuery = signal<string | null>(null);
  protected readonly hasFetched = signal(false);
  protected readonly pageNumber = signal(1);
  protected readonly totalPages = signal(0);

  protected readonly emptyMessage = computed(() => {
    const query = this.activeSearchQuery();
    return query ? `No companies found for "${query}".` : 'No companies have been added yet.';
  });

  protected onCompanyCreated(_company: Company): void {
    this.activeSearchQuery.set(null);
    // Re-fetch rather than append locally: the current page is alphabetically
    // ordered, so a locally-appended company would rarely belong at the end of
    // it, and totalCount/totalPages would otherwise go stale.
    this.loadCompanies();
  }

  protected onSearchRequested(query: string): void {
    if (this.searching() && this.inFlightSearchQuery === query) {
      return;
    }

    this.inFlightSearchQuery = query;
    this.searching.set(true);
    this.errorMessage.set(null);

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
