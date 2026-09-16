import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Company } from '../../../core/models/company.model';
import { CompanyList } from './company-list';

describe('CompanyList', () => {
  let fixture: ComponentFixture<CompanyList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompanyList],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanyList);
  });

  it('renders an empty state when there are no companies', () => {
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No companies have been added yet.');
  });

  it('renders company names', () => {
    fixture.componentRef.setInput('companies', [
      { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com' },
    ]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
  });

  it('renders website URLs as links with safe target attributes', () => {
    fixture.componentRef.setInput('companies', [
      { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com' },
    ]);
    fixture.detectChanges();

    const link = (fixture.nativeElement as HTMLElement).querySelector('a');
    expect(link?.getAttribute('href')).toBe('https://acme.com');
    expect(link?.getAttribute('target')).toBe('_blank');
    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('renders a custom empty message when provided', () => {
    fixture.componentRef.setInput('emptyMessage', 'No companies found for "micro".');
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No companies found for "micro".');
  });

  it('renders multiple companies', () => {
    const companies: Company[] = [
      { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com' },
      { id: '2', name: 'Globex', websiteUrl: 'https://globex.com' },
    ];
    fixture.componentRef.setInput('companies', companies);
    fixture.detectChanges();

    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('.company-list-item');
    expect(items.length).toBe(2);
  });
});
