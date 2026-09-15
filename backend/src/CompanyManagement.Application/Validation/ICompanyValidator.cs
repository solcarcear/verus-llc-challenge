namespace CompanyManagement.Application.Validation;

public interface ICompanyValidator
{
    CompanyValidationResult Validate(string? name, string? websiteUrl);
}
