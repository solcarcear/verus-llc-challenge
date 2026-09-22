export interface Company {
  readonly id: string;
  readonly name: string;
  readonly websiteUrl: string;
}

// What the paginated GET /api/companies browse list actually returns: name/website
// plus relationship counts, computed server-side so the UI never has to load full
// Contacts/Orders collections just to show "3 contacts, 16 orders" per row.
export interface CompanyListItem {
  readonly id: string;
  readonly name: string;
  readonly websiteUrl: string;
  readonly contactCount: number;
  readonly orderCount: number;
}

export interface CompanyDetails {
  readonly id: string;
  readonly name: string;
  readonly websiteUrl: string;
  readonly contacts: readonly Contact[];
  readonly orders: readonly Order[];
}

export interface Contact {
  readonly id: string;
  readonly companyId: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phone: string | null;
  readonly jobTitle: string | null;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export type OrderStatus = 'Pending' | 'Processing' | 'Completed' | 'Cancelled';

export interface Order {
  readonly id: string;
  readonly companyId: string;
  readonly orderNumber: string;
  readonly amount: number;
  readonly status: OrderStatus;
  readonly createdAt: string;
  readonly updatedAt: string | null;
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
