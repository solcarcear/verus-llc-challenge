import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { Order, OrderStatus, PagedResult } from '../models/company.model';

export interface OrderFilter {
  readonly companyId?: string;
  readonly status?: OrderStatus;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly ordersUrl = `${API_BASE_URL}/orders`;

  getAll(pageNumber: number, pageSize: number, filter: OrderFilter = {}): Observable<PagedResult<Order>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    if (filter.companyId) {
      params = params.set('companyId', filter.companyId);
    }

    if (filter.status) {
      params = params.set('status', filter.status);
    }

    return this.http.get<PagedResult<Order>>(this.ordersUrl, { params });
  }
}
