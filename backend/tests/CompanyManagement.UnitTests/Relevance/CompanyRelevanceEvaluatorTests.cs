using CompanyManagement.Application.Relevance;
using Xunit;

namespace CompanyManagement.UnitTests.Relevance;

public class CompanyRelevanceEvaluatorTests
{
    private readonly CompanyRelevanceEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsExactMatch_ForMicrosoftExample()
    {
        var result = _evaluator.Evaluate("Microsoft Corporation", "https://www.microsoft.com");

        Assert.True(result.IsRelevant);
        Assert.Equal(100, result.Score);
    }

    [Theory]
    [InlineData("Acme Inc")]
    [InlineData("Acme LLC")]
    [InlineData("Acme Ltd")]
    [InlineData("Acme Group")]
    [InlineData("Acme Corporation")]
    public void Evaluate_RemovesCorporateSuffixes_BeforeComparing(string companyName)
    {
        var result = _evaluator.Evaluate(companyName, "https://acme.com");

        Assert.True(result.IsRelevant);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Evaluate_IsCaseInsensitive()
    {
        var result = _evaluator.Evaluate("MICROSOFT", "HTTPS://MICROSOFT.COM");

        Assert.True(result.IsRelevant);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Evaluate_HandlesHyphenatedDomains_ForCocaColaExample()
    {
        var result = _evaluator.Evaluate("The Coca-Cola Company", "https://www.coca-cola.com");

        Assert.True(result.IsRelevant);
        Assert.True(result.Score >= 60);
    }

    [Theory]
    [InlineData("https://www.acme.com")]
    [InlineData("https://acme.com")]
    public void Evaluate_IgnoresWwwPrefix_RegardlessOfPresence(string websiteUrl)
    {
        var result = _evaluator.Evaluate("Acme", websiteUrl);

        Assert.True(result.IsRelevant);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Evaluate_ReturnsStrongContainmentMatch_WhenDomainIsPrefixOfCompanyIdentity()
    {
        var result = _evaluator.Evaluate("Acme Global", "https://acme.com");

        Assert.True(result.IsRelevant);
        Assert.Equal(80, result.Score);
    }

    [Fact]
    public void Evaluate_ReturnsMeaningfulTokenMatch_WhenWordsShareTokensButNotOrder()
    {
        var result = _evaluator.Evaluate("Acme Bottling Co", "https://bottling-acme.com");

        Assert.True(result.IsRelevant);
        Assert.Equal(60, result.Score);
    }

    [Fact]
    public void Evaluate_ReturnsNotRelevant_ForUnrelatedCompanyAndDomain()
    {
        var result = _evaluator.Evaluate("Microsoft Corporation", "https://apple.com");

        Assert.False(result.IsRelevant);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Evaluate_ProducesDescendingScores_AsMatchStrengthDecreases()
    {
        var exactMatch = _evaluator.Evaluate("Microsoft Corporation", "https://www.microsoft.com");
        var containmentMatch = _evaluator.Evaluate("Acme Global", "https://acme.com");
        var tokenMatch = _evaluator.Evaluate("Acme Bottling Co", "https://bottling-acme.com");
        var noMatch = _evaluator.Evaluate("Microsoft Corporation", "https://apple.com");

        Assert.True(exactMatch.Score > containmentMatch.Score);
        Assert.True(containmentMatch.Score > tokenMatch.Score);
        Assert.True(tokenMatch.Score > noMatch.Score);
    }

    [Theory]
    [InlineData("ftp://acme.com")]
    [InlineData("mailto:info@acme.com")]
    [InlineData("file:///acme.com")]
    public void Evaluate_ReturnsNotRelevant_ForUnsupportedUrlSchemes(string websiteUrl)
    {
        var result = _evaluator.Evaluate("Acme", websiteUrl);

        Assert.False(result.IsRelevant);
        Assert.Equal(0, result.Score);
    }

    [Theory]
    [InlineData(null, "https://acme.com")]
    [InlineData("", "https://acme.com")]
    [InlineData("Acme", null)]
    [InlineData("Acme", "")]
    [InlineData("Acme", "not-a-url")]
    public void Evaluate_ReturnsNotRelevant_ForMissingOrUnparsableInput(string? companyName, string? websiteUrl)
    {
        var result = _evaluator.Evaluate(companyName, websiteUrl);

        Assert.False(result.IsRelevant);
        Assert.Equal(0, result.Score);
    }
}
