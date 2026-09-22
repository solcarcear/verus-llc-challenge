export interface Company {
  readonly id: string;
  readonly name: string;
  readonly websiteUrl: string;
}

export interface CreateCompanyRequest {
  readonly name: string;
  readonly websiteUrl: string;
}

export interface UpdateCompanyRequest {
  readonly name: string;
  readonly websiteUrl: string;
}

export interface ApiErrorResponse {
  readonly message: string;
  readonly errors: readonly string[];
}

export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}
