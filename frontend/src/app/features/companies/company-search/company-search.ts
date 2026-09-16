import { Component, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

interface CompanySearchControls {
  query: FormControl<string>;
}

@Component({
  selector: 'app-company-search',
  imports: [ReactiveFormsModule],
  templateUrl: './company-search.html',
  styleUrl: './company-search.css',
})
export class CompanySearch {
  readonly searchRequested = output<string>();
  readonly clearRequested = output<void>();

  protected readonly form = new FormGroup<CompanySearchControls>({
    query: new FormControl('', { nonNullable: true }),
  });

  protected onSubmit(): void {
    const trimmed = this.form.controls.query.value.trim();

    if (!trimmed) {
      this.clear();
      return;
    }

    this.searchRequested.emit(trimmed);
  }

  protected onClear(): void {
    this.clear();
  }

  private clear(): void {
    this.form.controls.query.setValue('');
    this.clearRequested.emit();
  }
}
