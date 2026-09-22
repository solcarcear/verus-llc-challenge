namespace CompanyManagement.Application.Persistence;

// A lightweight read shape for list screens: just what the Company browse list
// needs (name/website plus relationship counts), not the full Contacts/Orders
// collections. Kept separate from the Company domain entity so counts - a
// presentation concern - never leak into the domain model itself.
public sealed record CompanySummary(Guid Id, string Name, string WebsiteUrl, int ContactCount, int OrderCount);
