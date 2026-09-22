import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { Contact, PagedResult } from '../models/company.model';

export interface ContactFilter {
  readonly companyId?: string;
  readonly isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly http = inject(HttpClient);
  private readonly contactsUrl = `${API_BASE_URL}/contacts`;

  getAll(pageNumber: number, pageSize: number, filter: ContactFilter = {}): Observable<PagedResult<Contact>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    if (filter.companyId) {
      params = params.set('companyId', filter.companyId);
    }

    if (filter.isActive !== undefined) {
      params = params.set('isActive', filter.isActive);
    }

    return this.http.get<PagedResult<Contact>>(this.contactsUrl, { params });
  }
}
