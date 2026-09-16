using CompanyManagement.Domain;

namespace CompanyManagement.Application.Services;

public interface ICompanyService
{
    Task<CreateCompanyResult> CreateCompanyAsync(string? name, string? websiteUrl, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetAllCompaniesAsync(CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> SearchCompaniesAsync(string? query, CancellationToken cancellationToken = default);
}
