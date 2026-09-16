using System.Text.RegularExpressions;

namespace CompanyManagement.Application.Relevance;

// Deterministic, offline domain-matching strategy: no HTTP calls or external
// services are involved. Matching is based purely on normalizing the company
// name and the website's host into comparable "identity" tokens.
//
// Known limitations:
// - Multi-part TLDs (e.g. ".co.uk") are not special-cased: only the single
//   last label is treated as the TLD, so "acme.co.uk" contributes the tokens
//   ["acme", "co"] rather than just ["acme"].
// - Subdomains other than "www" (e.g. "shop.acme.com") are treated as part of
//   the meaningful domain identity rather than being stripped.
public sealed class CompanyRelevanceEvaluator : ICompanyRelevanceEvaluator
{
    private const int ExactMatchScore = 100;
    private const int ContainmentMatchScore = 80;
    private const int TokenMatchScore = 60;
    private const int MinimumComparisonLength = 3;

    private static readonly HashSet<string> CorporateSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "company", "co", "corporation", "corp", "incorporated", "inc", "limited", "ltd", "llc", "group"
    };

    private static readonly Regex WordSplitter = new("[^A-Za-z0-9]+", RegexOptions.Compiled);

    public CompanyRelevanceResult Evaluate(string? companyName, string? websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(websiteUrl))
        {
            return CompanyRelevanceResult.NotRelevant();
        }

        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri))
        {
            return CompanyRelevanceResult.NotRelevant();
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return CompanyRelevanceResult.NotRelevant();
        }

        var companyTokens = ExtractCompanyTokens(companyName);
        var domainTokens = ExtractDomainTokens(uri.Host);

        if (companyTokens.Count == 0 || domainTokens.Count == 0)
        {
            return CompanyRelevanceResult.NotRelevant();
        }

        var companyIdentity = string.Concat(companyTokens);
        var domainIdentity = string.Concat(domainTokens);

        if (companyIdentity == domainIdentity)
        {
            return CompanyRelevanceResult.Relevant(ExactMatchScore);
        }

        var shorterLength = Math.Min(companyIdentity.Length, domainIdentity.Length);
        if (shorterLength >= MinimumComparisonLength &&
            (companyIdentity.Contains(domainIdentity, StringComparison.Ordinal) ||
             domainIdentity.Contains(companyIdentity, StringComparison.Ordinal)))
        {
            return CompanyRelevanceResult.Relevant(ContainmentMatchScore);
        }

        var hasSharedToken = companyTokens
            .Where(token => token.Length >= MinimumComparisonLength)
            .Intersect(domainTokens.Where(token => token.Length >= MinimumComparisonLength))
            .Any();

        return hasSharedToken
            ? CompanyRelevanceResult.Relevant(TokenMatchScore)
            : CompanyRelevanceResult.NotRelevant();
    }

    private static List<string> ExtractCompanyTokens(string companyName) =>
        SplitIntoWords(companyName)
            .Where(token => !CorporateSuffixes.Contains(token))
            .ToList();

    private static List<string> ExtractDomainTokens(string host)
    {
        var labels = host
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Where(label => !label.Equals("www", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (labels.Count > 1)
        {
            labels.RemoveAt(labels.Count - 1);
        }

        return labels.SelectMany(SplitIntoWords).ToList();
    }

    private static IEnumerable<string> SplitIntoWords(string value) =>
        WordSplitter.Split(value)
            .Where(token => token.Length > 0)
            .Select(token => token.ToLowerInvariant());
}
