import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CompanyListItem } from '../../../core/models/company.model';
import { CompanyList } from './company-list';

describe('CompanyList', () => {
  let fixture: ComponentFixture<CompanyList>;

  function findButton(label: string): HTMLButtonElement | null {
    const buttons = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.link-button'),
    );
    return buttons.find((button) => button.textContent?.trim() === label) ?? null;
  }

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
      { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com', contactCount: 0, orderCount: 0 },
    ]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Acme Corp');
  });

  it('renders website URLs as links with safe target attributes', () => {
    fixture.componentRef.setInput('companies', [
      { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com', contactCount: 0, orderCount: 0 },
    ]);
    fixture.detectChanges();

    const link = (fixture.nativeElement as HTMLElement).querySelector('a');
    expect(link?.getAttribute('href')).toBe('https://acme.com');
    expect(link?.getAttribute('target')).toBe('_blank');
    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('renders contact and order counts', () => {
    fixture.componentRef.setInput('companies', [
      { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com', contactCount: 3, orderCount: 12 },
    ]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('3 contacts');
    expect(text).toContain('12 orders');
  });

  it('renders a custom empty message when provided', () => {
    fixture.componentRef.setInput('emptyMessage', 'No companies found for "micro".');
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No companies found for "micro".');
  });

  it('renders multiple companies', () => {
    const companies: CompanyListItem[] = [
      { id: '1', name: 'Acme Corp', websiteUrl: 'https://acme.com', contactCount: 0, orderCount: 0 },
      { id: '2', name: 'Globex', websiteUrl: 'https://globex.com', contactCount: 0, orderCount: 0 },
    ];
    fixture.componentRef.setInput('companies', companies);
    fixture.detectChanges();

    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('.company-list-item');
    expect(items.length).toBe(2);
  });

  it('emits detailsRequested with the company when Details is clicked', () => {
    const company: CompanyListItem = {
      id: '1',
      name: 'Acme Corp',
      websiteUrl: 'https://acme.com',
      contactCount: 0,
      orderCount: 0,
    };
    fixture.componentRef.setInput('companies', [company]);
    const emitted: CompanyListItem[] = [];
    fixture.componentInstance.detailsRequested.subscribe((c) => emitted.push(c));
    fixture.detectChanges();

    findButton('Details')?.click();

    expect(emitted).toEqual([company]);
  });

  it('emits editRequested with the company when Edit is clicked', () => {
    const company: CompanyListItem = {
      id: '1',
      name: 'Acme Corp',
      websiteUrl: 'https://acme.com',
      contactCount: 0,
      orderCount: 0,
    };
    fixture.componentRef.setInput('companies', [company]);
    const emitted: CompanyListItem[] = [];
    fixture.componentInstance.editRequested.subscribe((c) => emitted.push(c));
    fixture.detectChanges();

    findButton('Edit')?.click();

    expect(emitted).toEqual([company]);
  });

  it('emits deleteRequested with the company when Delete is clicked', () => {
    const company: CompanyListItem = {
      id: '1',
      name: 'Acme Corp',
      websiteUrl: 'https://acme.com',
      contactCount: 0,
      orderCount: 0,
    };
    fixture.componentRef.setInput('companies', [company]);
    const emitted: CompanyListItem[] = [];
    fixture.componentInstance.deleteRequested.subscribe((c) => emitted.push(c));
    fixture.detectChanges();

    findButton('Delete')?.click();

    expect(emitted).toEqual([company]);
  });
});
