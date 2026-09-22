import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { CompanyService } from '../../../core/services/company.service';
import { Company, UpdateCompanyRequest } from '../../../core/models/company.model';
import { CompanyEditModal } from './company-edit-modal';

describe('CompanyEditModal', () => {
  let fixture: ComponentFixture<CompanyEditModal>;
  let component: CompanyEditModal;
  let updateSpy: ReturnType<typeof vi.fn>;

  const company: Company = { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com' };

  function submit(): void {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  beforeEach(async () => {
    updateSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [CompanyEditModal],
      providers: [{ provide: CompanyService, useValue: { update: updateSpy } }],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanyEditModal);
    fixture.componentRef.setInput('company', company);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('pre-fills the form with the selected company', () => {
    expect(component['form'].controls.name.value).toBe('Acme Corp');
    expect(component['form'].controls.websiteUrl.value).toBe('https://acme.com');
  });

  it('requires a company name', () => {
    component['form'].controls.name.setValue('');
    submit();

    expect(component['form'].controls.name.hasError('required')).toBe(true);
    expect(updateSpy).not.toHaveBeenCalled();
  });

  it('rejects an invalid website URL', () => {
    component['form'].controls.websiteUrl.setValue('not-a-url');
    submit();

    expect(component['form'].controls.websiteUrl.hasError('invalidUrl')).toBe(true);
    expect(updateSpy).not.toHaveBeenCalled();
  });

  it('calls CompanyService.update with the company id and the edited values on save', () => {
    updateSpy.mockReturnValue(new Subject<Company>());
    component['form'].setValue({ name: 'Acme Global', websiteUrl: 'https://acmeglobal.com' });

    submit();

    const expected: UpdateCompanyRequest = { name: 'Acme Global', websiteUrl: 'https://acmeglobal.com' };
    expect(updateSpy).toHaveBeenCalledWith('1', expected);
  });

  it('emits saved with the updated company on success', () => {
    const subject = new Subject<Company>();
    updateSpy.mockReturnValue(subject);
    const updated: Company = { id: '1', name: 'Acme Global', websiteUrl: 'https://acmeglobal.com' };
    const emitted: Company[] = [];
    component.saved.subscribe((c) => emitted.push(c));

    submit();
    subject.next(updated);

    expect(emitted).toEqual([updated]);
  });

  it('emits cancelled and calls no API when Cancel is clicked', () => {
    let cancelled = false;
    component.cancelled.subscribe(() => (cancelled = true));

    const cancelButton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      'button.secondary',
    );
    cancelButton?.click();

    expect(cancelled).toBe(true);
    expect(updateSpy).not.toHaveBeenCalled();
  });

  it('displays the backend validation message on a 400 response', () => {
    const subject = new Subject<Company>();
    updateSpy.mockReturnValue(subject);
    submit();

    subject.error(
      new HttpErrorResponse({
        status: 400,
        error: { message: 'Company validation failed.', errors: ['Company name is required.'] },
      }),
    );
    fixture.detectChanges();

    expect(component['backendErrors']()).toEqual(['Company name is required.']);
  });

  it('displays a not-found message on a 404 response', () => {
    const subject = new Subject<Company>();
    updateSpy.mockReturnValue(subject);
    submit();

    subject.error(new HttpErrorResponse({ status: 404 }));
    fixture.detectChanges();

    expect(component['genericErrorMessage']()).toBe(
      'This company no longer exists. It may have already been deleted.',
    );
  });

  it('displays a generic message on an unexpected error', () => {
    const subject = new Subject<Company>();
    updateSpy.mockReturnValue(subject);
    submit();

    subject.error(new HttpErrorResponse({ status: 500 }));
    fixture.detectChanges();

    expect(component['genericErrorMessage']()).toBe('Something went wrong. Please try again later.');
  });
});
