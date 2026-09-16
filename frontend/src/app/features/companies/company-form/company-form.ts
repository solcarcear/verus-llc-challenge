import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { CompanyService } from '../../../core/services/company.service';
import { ApiErrorResponse, Company } from '../../../core/models/company.model';

const MINIMUM_NAME_LENGTH = 3;

function httpUrlValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value?.trim();

  if (!value) {
    return null;
  }

  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:' ? null : { invalidUrl: true };
  } catch {
    return { invalidUrl: true };
  }
}

interface CompanyFormControls {
  name: FormControl<string>;
  websiteUrl: FormControl<string>;
}

@Component({
  selector: 'app-company-form',
  imports: [ReactiveFormsModule],
  templateUrl: './company-form.html',
  styleUrl: './company-form.css',
})
export class CompanyForm {
  private readonly companyService = inject(CompanyService);

  readonly created = output<Company>();

  protected readonly minimumNameLength = MINIMUM_NAME_LENGTH;
  protected readonly isSubmitting = signal(false);
  protected readonly submitAttempted = signal(false);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly genericErrorMessage = signal<string | null>(null);
  protected readonly backendErrors = signal<readonly string[] | null>(null);

  protected readonly form = new FormGroup<CompanyFormControls>({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(MINIMUM_NAME_LENGTH)],
    }),
    websiteUrl: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, httpUrlValidator],
    }),
  });

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.genericErrorMessage.set(null);
      this.backendErrors.set(null);
    });
  }

  protected showError(controlName: keyof CompanyFormControls): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.touched || this.submitAttempted());
  }

  protected onSubmit(): void {
    this.submitAttempted.set(true);
    this.successMessage.set(null);
    this.genericErrorMessage.set(null);
    this.backendErrors.set(null);

    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const { name, websiteUrl } = this.form.getRawValue();

    this.companyService.create({ name: name.trim(), websiteUrl: websiteUrl.trim() }).subscribe({
      next: (company) => {
        this.isSubmitting.set(false);
        this.submitAttempted.set(false);
        this.successMessage.set(`"${company.name}" was created successfully.`);
        this.form.reset();
        this.created.emit(company);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.handleError(error);
      },
    });
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 400) {
      const apiError = error.error as ApiErrorResponse | null;
      const messages = apiError?.errors?.length
        ? apiError.errors
        : apiError?.message
          ? [apiError.message]
          : ['The company could not be created. Please check your input.'];
      this.backendErrors.set(messages);
      return;
    }

    this.genericErrorMessage.set('Something went wrong. Please try again later.');
  }
}
