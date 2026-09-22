import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { OrderService } from '../../../core/services/order.service';
import { Order, OrderStatus } from '../../../core/models/company.model';

type StatusFilter = 'all' | OrderStatus;

// Self-contained, same reasoning as ContactList - see that component's header
// comment. No Order create/edit/delete UI exists yet (nor does the backend
// expose it), so this is a read-only browse screen.
@Component({
  selector: 'app-order-list',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './order-list.html',
  styleUrl: './order-list.css',
})
export class OrderList implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly pageSize = 20;

  protected readonly orders = signal<readonly Order[]>([]);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pageNumber = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly totalCount = signal(0);
  protected readonly statusFilter = signal<StatusFilter>('all');

  ngOnInit(): void {
    this.loadOrders();
  }

  protected onFilterChange(event: Event): void {
    this.statusFilter.set((event.target as HTMLSelectElement).value as StatusFilter);
    this.pageNumber.set(1);
    this.loadOrders();
  }

  protected onPreviousPage(): void {
    if (this.pageNumber() <= 1) {
      return;
    }

    this.pageNumber.update((current) => current - 1);
    this.loadOrders();
  }

  protected onNextPage(): void {
    if (this.pageNumber() >= this.totalPages()) {
      return;
    }

    this.pageNumber.update((current) => current + 1);
    this.loadOrders();
  }

  private loadOrders(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const filter = this.statusFilter();
    const status = filter === 'all' ? undefined : filter;

    this.orderService.getAll(this.pageNumber(), this.pageSize, { status }).subscribe({
      next: (page) => {
        this.orders.set(page.items);
        this.totalPages.set(page.totalPages);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load orders.');
        this.loading.set(false);
      },
    });
  }
}
