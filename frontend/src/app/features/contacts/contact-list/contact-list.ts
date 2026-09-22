import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ContactService } from '../../../core/services/contact.service';
import { Contact } from '../../../core/models/company.model';

type ActiveFilter = 'all' | 'active' | 'inactive';

// Self-contained, like App is for companies: owns its own paginated fetch,
// filter, loading, and error state instead of routing everything through
// App. There's no Contact create/edit/delete UI yet (the backend doesn't
// expose it either - see ContactsController), so this is a read-only browse
// screen; App just decides when it's visible (see the tab switcher in app.ts).
@Component({
  selector: 'app-contact-list',
  imports: [DatePipe],
  templateUrl: './contact-list.html',
  styleUrl: './contact-list.css',
})
export class ContactList implements OnInit {
  private readonly contactService = inject(ContactService);
  private readonly pageSize = 20;

  protected readonly contacts = signal<readonly Contact[]>([]);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pageNumber = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly totalCount = signal(0);
  protected readonly activeFilter = signal<ActiveFilter>('all');

  ngOnInit(): void {
    this.loadContacts();
  }

  protected onFilterChange(event: Event): void {
    this.activeFilter.set((event.target as HTMLSelectElement).value as ActiveFilter);
    this.pageNumber.set(1);
    this.loadContacts();
  }

  protected onPreviousPage(): void {
    if (this.pageNumber() <= 1) {
      return;
    }

    this.pageNumber.update((current) => current - 1);
    this.loadContacts();
  }

  protected onNextPage(): void {
    if (this.pageNumber() >= this.totalPages()) {
      return;
    }

    this.pageNumber.update((current) => current + 1);
    this.loadContacts();
  }

  private loadContacts(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const filter = this.activeFilter();
    const isActive = filter === 'all' ? undefined : filter === 'active';

    this.contactService.getAll(this.pageNumber(), this.pageSize, { isActive }).subscribe({
      next: (page) => {
        this.contacts.set(page.items);
        this.totalPages.set(page.totalPages);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load contacts.');
        this.loading.set(false);
      },
    });
  }
}
