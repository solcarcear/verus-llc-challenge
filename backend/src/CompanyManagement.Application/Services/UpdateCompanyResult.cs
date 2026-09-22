using CompanyManagement.Domain;

namespace CompanyManagement.Application.Services;

public enum CompanyUpdateStatus
{
    Updated,
    NotFound,
    ValidationFailed,
    NotRelevant
}

public sealed record UpdateCompanyResult(CompanyUpdateStatus Status, Company? Company, IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Status == CompanyUpdateStatus.Updated;

    public static UpdateCompanyResult Success(Company company) =>
        new(CompanyUpdateStatus.Updated, company, Array.Empty<string>());

    public static UpdateCompanyResult NotFound() =>
        new(CompanyUpdateStatus.NotFound, null, Array.Empty<string>());

    public static UpdateCompanyResult ValidationFailure(IReadOnlyList<string> errors) =>
        new(CompanyUpdateStatus.ValidationFailed, null, errors);

    public static UpdateCompanyResult RelevanceFailure(string message) =>
        new(CompanyUpdateStatus.NotRelevant, null, new[] { message });
}
