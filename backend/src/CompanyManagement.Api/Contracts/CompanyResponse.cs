using CompanyManagement.Domain;

namespace CompanyManagement.Api.Contracts;

public sealed record CompanyResponse(Guid Id, string Name, string WebsiteUrl)
{
    public static CompanyResponse FromDomain(Company company) =>
        new(company.Id, company.Name, company.WebsiteUrl);
}
