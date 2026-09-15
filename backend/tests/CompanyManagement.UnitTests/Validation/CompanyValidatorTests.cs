using CompanyManagement.Application.Validation;
using Xunit;

namespace CompanyManagement.UnitTests.Validation;

public class CompanyValidatorTests
{
    private readonly CompanyValidator _validator = new();

    [Theory]
    [InlineData("Acme Corp", "https://acme.com")]
    [InlineData("Acme Corp", "http://acme.com")]
    [InlineData("Abc", "https://acme.com")]
    public void Validate_ReturnsSuccess_ForValidNameAndUrl(string name, string websiteUrl)
    {
        var result = _validator.Validate(name, websiteUrl);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Fails_WhenNameIsMissingOrWhitespace(string? name)
    {
        var result = _validator.Validate(name, "https://acme.com");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Fails_WhenNameIsBelowMinimumLength()
    {
        var result = _validator.Validate("A", "https://acme.com");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Fails_WhenWebsiteUrlIsMissingOrWhitespace(string? websiteUrl)
    {
        var result = _validator.Validate("Acme Corp", websiteUrl);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("www.acme.com")]
    [InlineData("ftp://acme.com")]
    [InlineData("/relative/path")]
    public void Validate_Fails_WhenWebsiteUrlIsNotAnAbsoluteHttpOrHttpsUrl(string websiteUrl)
    {
        var result = _validator.Validate("Acme Corp", websiteUrl);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("HTTP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsAllApplicableErrors_WhenBothFieldsAreInvalid()
    {
        var result = _validator.Validate(" ", "not-a-url");

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }
}
