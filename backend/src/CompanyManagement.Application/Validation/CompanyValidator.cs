namespace CompanyManagement.Application.Validation;

public sealed class CompanyValidator : ICompanyValidator
{
    private const int MinimumNameLength = 3;

    public CompanyValidationResult Validate(string? name, string? websiteUrl)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Company name is required.");
        }
        else if (name.Trim().Length < MinimumNameLength)
        {
            errors.Add($"Company name must be at least {MinimumNameLength} characters long.");
        }

        if (string.IsNullOrWhiteSpace(websiteUrl))
        {
            errors.Add("Website URL is required.");
        }
        else if (!IsValidHttpUrl(websiteUrl))
        {
            errors.Add("Website URL must be a valid absolute HTTP or HTTPS URL.");
        }

        return errors.Count == 0
            ? CompanyValidationResult.Success()
            : CompanyValidationResult.Failure(errors.ToArray());
    }

    private static bool IsValidHttpUrl(string websiteUrl)
    {
        return Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
