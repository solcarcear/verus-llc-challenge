using CompanyManagement.Domain;

namespace CompanyManagement.Api.Contracts;

public sealed record CompanyDetailsResponse(
    Guid Id,
    string Name,
    string WebsiteUrl,
    IReadOnlyList<ContactResponse> Contacts,
    IReadOnlyList<OrderResponse> Orders)
{
    public static CompanyDetailsResponse FromDomain(Company company) =>
        new(
            company.Id,
            company.Name,
            company.WebsiteUrl,
            company.Contacts.Select(ContactResponse.FromDomain).ToList(),
            company.Orders.Select(OrderResponse.FromDomain).ToList());
}
