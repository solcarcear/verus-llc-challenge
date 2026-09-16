import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { CompanyService } from '../../../core/services/company.service';
import { Company, CreateCompanyRequest } from '../../../core/models/company.model';
import { CompanyForm } from './company-form';

describe('CompanyForm', () => {
  let fixture: ComponentFixture<CompanyForm>;
  let component: CompanyForm;
  let createSpy: ReturnType<typeof vi.fn>;

  const validName = 'Acme Corp';
  const validWebsite = 'https://acme.com';

  function setValue(name: string, websiteUrl: string): void {
    component['form'].setValue({ name, websiteUrl });
  }

  function submit(): void {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  beforeEach(async () => {
    createSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [CompanyForm],
      providers: [{ provide: CompanyService, useValue: { create: createSpy } }],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanyForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('starts invalid', () => {
    expect(component['form'].valid).toBe(false);
  });

  it('requires company name', () => {
    setValue('', validWebsite);
    submit();

    expect(component['form'].controls.name.hasError('required')).toBe(true);
    expect(createSpy).not.toHaveBeenCalled();
  });

  it('enforces the minimum company name length', () => {
    setValue('AB', validWebsite);
    submit();

    expect(component['form'].controls.name.hasError('minlength')).toBe(true);
    expect(createSpy).not.toHaveBeenCalled();
  });

  it('requires website URL', () => {
    setValue(validName, '');
    submit();

    expect(component['form'].controls.websiteUrl.hasError('required')).toBe(true);
    expect(createSpy).not.toHaveBeenCalled();
  });

  it('calls CompanyService.create with the expected request when the form is valid', () => {
    createSpy.mockReturnValue(new Subject<Company>());
    setValue(validName, validWebsite);

    submit();

    const expected: CreateCompanyRequest = { name: validName, websiteUrl: validWebsite };
    expect(createSpy).toHaveBeenCalledWith(expected);
  });

  it('resets the form on successful creation', () => {
    const subject = new Subject<Company>();
    createSpy.mockReturnValue(subject);
    setValue(validName, validWebsite);
    submit();

    subject.next({ id: '1', name: validName, websiteUrl: validWebsite });
    fixture.detectChanges();

    expect(component['form'].controls.name.value).toBe('');
    expect(component['form'].controls.websiteUrl.value).toBe('');
  });

  it('emits the created company on successful creation', () => {
    const subject = new Subject<Company>();
    createSpy.mockReturnValue(subject);
    const createdCompany: Company = { id: '1', name: validName, websiteUrl: validWebsite };
    const emitted: Company[] = [];
    component.created.subscribe((company) => emitted.push(company));

    setValue(validName, validWebsite);
    submit();
    subject.next(createdCompany);

    expect(emitted).toEqual([createdCompany]);
  });

  it('prevents duplicate submissions while a request is pending', () => {
    const subject = new Subject<Company>();
    createSpy.mockReturnValue(subject);
    setValue(validName, validWebsite);

    submit();
    submit();

    expect(createSpy).toHaveBeenCalledTimes(1);
  });

  it('displays the backend validation message on a 400 response', () => {
    const subject = new Subject<Company>();
    createSpy.mockReturnValue(subject);
    setValue(validName, validWebsite);
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

  it('displays a generic message on an unexpected error', () => {
    const subject = new Subject<Company>();
    createSpy.mockReturnValue(subject);
    setValue(validName, validWebsite);
    submit();

    subject.error(new HttpErrorResponse({ status: 500 }));
    fixture.detectChanges();

    expect(component['genericErrorMessage']()).toBe('Something went wrong. Please try again later.');
  });

  it('clears the stale backend error once the user starts correcting the form', () => {
    const subject = new Subject<Company>();
    createSpy.mockReturnValue(subject);
    setValue(validName, validWebsite);
    submit();

    subject.error(
      new HttpErrorResponse({
        status: 400,
        error: { message: 'Company validation failed.', errors: ['Company name is required.'] },
      }),
    );
    fixture.detectChanges();
    expect(component['backendErrors']()).not.toBeNull();

    component['form'].controls.name.setValue('Acme Updated');
    fixture.detectChanges();

    expect(component['backendErrors']()).toBeNull();
  });
});
