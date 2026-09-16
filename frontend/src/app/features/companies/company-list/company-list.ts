import { Component, input } from '@angular/core';
import { Company } from '../../../core/models/company.model';

@Component({
  selector: 'app-company-list',
  templateUrl: './company-list.html',
  styleUrl: './company-list.css',
})
export class CompanyList {
  readonly companies = input<readonly Company[]>([]);
}
