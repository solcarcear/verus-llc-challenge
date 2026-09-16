export interface Company {
  readonly id: string;
  readonly name: string;
  readonly websiteUrl: string;
}

export interface CreateCompanyRequest {
  readonly name: string;
  readonly websiteUrl: string;
}

export interface ApiErrorResponse {
  readonly message: string;
  readonly errors: readonly string[];
}
