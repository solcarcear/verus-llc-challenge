import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CompanySearch } from './company-search';

describe('CompanySearch', () => {
  let fixture: ComponentFixture<CompanySearch>;
  let component: CompanySearch;

  function setInputValue(value: string): void {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function submitForm(): void {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
  }

  function clickClear(): void {
    const buttons = fixture.nativeElement.querySelectorAll('button');
    const clearButton = Array.from(buttons).find(
      (button) => (button as HTMLButtonElement).type === 'button',
    ) as HTMLButtonElement;
    clearButton.click();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompanySearch],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanySearch);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('emits a trimmed search query on submission', () => {
    const emitted: string[] = [];
    component.searchRequested.subscribe((query) => emitted.push(query));

    setInputValue('  acme  ');
    submitForm();

    expect(emitted).toEqual(['acme']);
  });

  it('emits clearRequested instead of searchRequested when submitting whitespace only', () => {
    const searched: string[] = [];
    let cleared = false;
    component.searchRequested.subscribe((query) => searched.push(query));
    component.clearRequested.subscribe(() => (cleared = true));

    setInputValue('   ');
    submitForm();

    expect(searched).toEqual([]);
    expect(cleared).toBe(true);
  });

  it('triggers a search on form submission (Enter key)', () => {
    const emitted: string[] = [];
    component.searchRequested.subscribe((query) => emitted.push(query));

    setInputValue('globex');
    submitForm();

    expect(emitted).toEqual(['globex']);
  });

  it('emits clearRequested when the Clear button is clicked', () => {
    let cleared = false;
    component.clearRequested.subscribe(() => (cleared = true));

    setInputValue('acme');
    clickClear();

    expect(cleared).toBe(true);
  });

  it('resets the input when Clear is clicked', () => {
    setInputValue('acme');
    clickClear();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(input.value).toBe('');
  });
});
