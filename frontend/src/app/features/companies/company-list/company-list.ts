import { Component, input, output } from '@angular/core';
import { CompanyListItem } from '../../../core/models/company.model';

@Component({
  selector: 'app-company-list',
  templateUrl: './company-list.html',
  styleUrl: './company-list.css',
})
export class CompanyList {
  readonly companies = input<readonly CompanyListItem[]>([]);
  readonly emptyMessage = input<string>('No companies have been added yet.');

  // Child -> parent: CompanyList doesn't call the API or know about the modal
  // or pagination itself - it just reports which company the user wants to
  // view, edit, or delete, and lets App (the coordinator) decide what to do.
  readonly detailsRequested = output<CompanyListItem>();
  readonly editRequested = output<CompanyListItem>();
  readonly deleteRequested = output<CompanyListItem>();

  protected onDetails(company: CompanyListItem): void {
    this.detailsRequested.emit(company);
  }

  protected onEdit(company: CompanyListItem): void {
    this.editRequested.emit(company);
  }

  protected onDelete(company: CompanyListItem): void {
    this.deleteRequested.emit(company);
  }
}
