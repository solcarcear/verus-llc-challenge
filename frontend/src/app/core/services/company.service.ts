import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import {
  Company,
  CompanyDetails,
  CompanyListItem,
  CreateCompanyRequest,
  PagedResult,
  UpdateCompanyRequest,
} from '../models/company.model';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private readonly http = inject(HttpClient);
  private readonly companiesUrl = `${API_BASE_URL}/companies`;

  getAll(pageNumber: number, pageSize: number): Observable<PagedResult<CompanyListItem>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<CompanyListItem>>(this.companiesUrl, { params });
  }

  getById(id: string): Observable<Company> {
    return this.http.get<Company>(`${this.companiesUrl}/${id}`);
  }

  getDetails(id: string): Observable<CompanyDetails> {
    return this.http.get<CompanyDetails>(`${this.companiesUrl}/${id}/details`);
  }

  create(request: CreateCompanyRequest): Observable<Company> {
    return this.http.post<Company>(this.companiesUrl, request);
  }

  search(query: string): Observable<CompanyListItem[]> {
    const params = new HttpParams().set('search', query);
    return this.http.get<CompanyListItem[]>(this.companiesUrl, { params });
  }

  update(id: string, request: UpdateCompanyRequest): Observable<Company> {
    return this.http.put<Company>(`${this.companiesUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.companiesUrl}/${id}`);
  }
}
