import { Component, input, output } from '@angular/core';
import { Company } from '../../../core/models/company.model';

@Component({
  selector: 'app-company-list',
  templateUrl: './company-list.html',
  styleUrl: './company-list.css',
})
export class CompanyList {
  readonly companies = input<readonly Company[]>([]);
  readonly emptyMessage = input<string>('No companies have been added yet.');

  // Child -> parent: CompanyList doesn't call the API or know about the modal
  // or pagination itself - it just reports which company the user wants to
  // edit or delete, and lets App (the coordinator) decide what to do about it.
  readonly editRequested = output<Company>();
  readonly deleteRequested = output<Company>();

  protected onEdit(company: Company): void {
    this.editRequested.emit(company);
  }

  protected onDelete(company: Company): void {
    this.deleteRequested.emit(company);
  }
}
