namespace CompanyManagement.Application.Validation;

public sealed record CompanyValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static CompanyValidationResult Success() => new(true, Array.Empty<string>());

    public static CompanyValidationResult Failure(params string[] errors) => new(false, errors);
}
