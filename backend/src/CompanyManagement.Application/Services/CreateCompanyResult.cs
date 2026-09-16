using CompanyManagement.Domain;

namespace CompanyManagement.Application.Services;

public enum CompanyCreationStatus
{
    Created,
    ValidationFailed,
    NotRelevant
}

public sealed record CreateCompanyResult(CompanyCreationStatus Status, Company? Company, IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Status == CompanyCreationStatus.Created;

    public static CreateCompanyResult Success(Company company) =>
        new(CompanyCreationStatus.Created, company, Array.Empty<string>());

    public static CreateCompanyResult ValidationFailure(IReadOnlyList<string> errors) =>
        new(CompanyCreationStatus.ValidationFailed, null, errors);

    public static CreateCompanyResult RelevanceFailure(string message) =>
        new(CompanyCreationStatus.NotRelevant, null, new[] { message });
}
