using CompanyManagement.Application.Persistence;

namespace CompanyManagement.Api.Contracts;

// Used for the paginated company browse list - name/website plus relationship
// counts, not the full Contacts/Orders collections (see CompanySummary).
public sealed record CompanyListItemResponse(Guid Id, string Name, string WebsiteUrl, int ContactCount, int OrderCount)
{
    public static CompanyListItemResponse FromSummary(CompanySummary summary) =>
        new(summary.Id, summary.Name, summary.WebsiteUrl, summary.ContactCount, summary.OrderCount);
}
