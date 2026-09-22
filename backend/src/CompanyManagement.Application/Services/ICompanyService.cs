using CompanyManagement.Application.Persistence;
using CompanyManagement.Domain;

namespace CompanyManagement.Application.Services;

public interface ICompanyService
{
    Task<CreateCompanyResult> CreateCompanyAsync(string? name, string? websiteUrl, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetAllCompaniesAsync(CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Company> Items, int TotalCount)> GetCompaniesPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> SearchCompaniesAsync(string? query, CancellationToken cancellationToken = default);

    Task<UpdateCompanyResult> UpdateCompanyAsync(
        Guid id,
        string? name,
        string? websiteUrl,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCompanyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CompanySummary> Items, int TotalCount)> GetCompanySummariesPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, CompanyRelationshipCounts>> GetRelationshipCountsAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken = default);
}
