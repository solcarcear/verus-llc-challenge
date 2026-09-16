import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CompanyForm } from './features/companies/company-form/company-form';
import { CompanyList } from './features/companies/company-list/company-list';
import { CompanyService } from './core/services/company.service';
import { Company } from './core/models/company.model';

@Component({
  imports: [RouterOutlet, CompanyForm, CompanyList],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly companyService = inject(CompanyService);

  protected readonly companies = signal<readonly Company[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal(false);

  ngOnInit(): void {
    this.loading.set(true);
    this.loadError.set(false);

    this.companyService.getAll().subscribe({
      next: (companies) => {
        this.companies.set(companies);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  protected onCompanyCreated(company: Company): void {
    this.companies.update((current) => [...current, company]);
  }
}
