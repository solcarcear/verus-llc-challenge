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
  private inFlightSearchQuery: string | null = null;

  protected readonly companies = signal<readonly Company[]>([]);
  protected readonly loading = signal(false);
  protected readonly searching = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly activeSearchQuery = signal<string | null>(null);
  protected readonly hasFetched = signal(false);

  protected readonly emptyMessage = computed(() => {
    const query = this.activeSearchQuery();
    return query ? `No companies found for "${query}".` : 'No companies have been added yet.';
  });

  protected onCompanyCreated(company: Company): void {
    if (this.activeSearchQuery()) {
      this.activeSearchQuery.set(null);
      this.loadCompanies();
      return;
    }

    this.hasFetched.set(true);
    this.companies.update((current) => [...current, company]);
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
    this.loadCompanies();
  }

  protected loadCompanies(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.companyService.getAll().subscribe({
      next: (companies) => {
        this.companies.set(companies);
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
