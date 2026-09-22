import { AbstractControl, ValidationErrors } from '@angular/forms';

// Shared by CompanyForm and CompanyEditModal, which both collect the same
// Website URL field and need it to look like a real, absolute http(s) URL.
export function httpUrlValidator(control: AbstractControl<string>): ValidationErrors | null {
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
