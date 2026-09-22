import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { CompanyService } from '../../../core/services/company.service';
import { CompanyDetails, CompanyListItem } from '../../../core/models/company.model';

// Parent -> child: App renders this only while a company's details are being
// viewed and passes in just the selected row (id + name are enough to show a
// heading immediately) via input.required.
// Child -> parent: `closed` is the only thing this reports back - viewing
// details never changes the company list or pagination, so there's nothing
// else for App to react to.
@Component({
  selector: 'app-company-details-modal',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './company-details-modal.html',
  styleUrl: './company-details-modal.css',
})
export class CompanyDetailsModal implements OnInit {
  private readonly companyService = inject(CompanyService);

  readonly company = input.required<CompanyListItem>();
  readonly closed = output<void>();

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly details = signal<CompanyDetails | null>(null);

  ngOnInit(): void {
    this.companyService.getDetails(this.company().id).subscribe({
      next: (details) => {
        this.details.set(details);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load company details.');
        this.loading.set(false);
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
