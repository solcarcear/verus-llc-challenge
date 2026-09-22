import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CompanyService } from '../../../core/services/company.service';
import { httpUrlValidator } from '../../../core/validators/http-url.validator';
import { ApiErrorResponse, Company } from '../../../core/models/company.model';

const MINIMUM_NAME_LENGTH = 3;

interface CompanyEditControls {
  name: FormControl<string>;
  websiteUrl: FormControl<string>;
}

// Parent -> child: App renders this component only while a company is being
// edited and passes it in via `company` (input.required, since the modal has
// nothing meaningful to show without one).
// Child -> parent: `saved` and `cancelled` are how this component tells App
// what happened, without knowing anything about the company list or paging -
// App decides what "close the modal" and "refresh the list" actually mean.
@Component({
  selector: 'app-company-edit-modal',
  imports: [ReactiveFormsModule],
  templateUrl: './company-edit-modal.html',
  styleUrl: './company-edit-modal.css',
})
export class CompanyEditModal implements OnInit {
  private readonly companyService = inject(CompanyService);

  readonly company = input.required<Company>();
  readonly saved = output<Company>();
  readonly cancelled = output<void>();

  protected readonly minimumNameLength = MINIMUM_NAME_LENGTH;
  protected readonly isSubmitting = signal(false);
  protected readonly genericErrorMessage = signal<string | null>(null);
  protected readonly backendErrors = signal<readonly string[] | null>(null);

  // Built in ngOnInit rather than a field initializer: Angular's compiler
  // only guarantees a required input has its value by the time lifecycle
  // hooks run, not during field initialization.
  protected form!: FormGroup<CompanyEditControls>;

  ngOnInit(): void {
    const company = this.company();

    this.form = new FormGroup<CompanyEditControls>({
      name: new FormControl(company.name, {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(MINIMUM_NAME_LENGTH)],
      }),
      websiteUrl: new FormControl(company.websiteUrl, {
        nonNullable: true,
        validators: [Validators.required, httpUrlValidator],
      }),
    });
  }

  protected showError(controlName: keyof CompanyEditControls): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && control.touched;
  }

  protected onSave(): void {
    this.genericErrorMessage.set(null);
    this.backendErrors.set(null);

    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const { name, websiteUrl } = this.form.getRawValue();

    this.companyService.update(this.company().id, { name: name.trim(), websiteUrl: websiteUrl.trim() }).subscribe({
      next: (updated) => {
        this.isSubmitting.set(false);
        this.saved.emit(updated);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.handleError(error);
      },
    });
  }

  protected onCancel(): void {
    this.cancelled.emit();
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 400) {
      const apiError = error.error as ApiErrorResponse | null;
      const messages = apiError?.errors?.length
        ? apiError.errors
        : apiError?.message
          ? [apiError.message]
          : ['The company could not be updated. Please check your input.'];
      this.backendErrors.set(messages);
      return;
    }

    if (error.status === 404) {
      this.genericErrorMessage.set('This company no longer exists. It may have already been deleted.');
      return;
    }

    this.genericErrorMessage.set('Something went wrong. Please try again later.');
  }
}
