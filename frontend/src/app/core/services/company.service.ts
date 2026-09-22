import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { Company, CreateCompanyRequest, PagedResult, UpdateCompanyRequest } from '../models/company.model';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private readonly http = inject(HttpClient);
  private readonly companiesUrl = `${API_BASE_URL}/companies`;

  getAll(pageNumber: number, pageSize: number): Observable<PagedResult<Company>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<Company>>(this.companiesUrl, { params });
  }

  getById(id: string): Observable<Company> {
    return this.http.get<Company>(`${this.companiesUrl}/${id}`);
  }

  create(request: CreateCompanyRequest): Observable<Company> {
    return this.http.post<Company>(this.companiesUrl, request);
  }

  search(query: string): Observable<Company[]> {
    const params = new HttpParams().set('search', query);
    return this.http.get<Company[]>(this.companiesUrl, { params });
  }

  update(id: string, request: UpdateCompanyRequest): Observable<Company> {
    return this.http.put<Company>(`${this.companiesUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.companiesUrl}/${id}`);
  }
}
