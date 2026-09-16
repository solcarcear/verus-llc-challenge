using CompanyManagement.Application.Persistence;
using CompanyManagement.Application.Relevance;
using CompanyManagement.Application.Validation;
using CompanyManagement.Domain;

namespace CompanyManagement.Application.Services;

public sealed class CompanyService : ICompanyService
{
    private const string NotRelevantMessage = "Company name is not sufficiently relevant to the provided website.";

    // Search relevance scoring: answers "how closely does this stored company
    // match the user's search query?" This is intentionally separate from
    // ICompanyRelevanceEvaluator, which answers a different question ("does
    // this company name reasonably correspond to this website?") at creation
    // time using identity/token normalization. Search here uses plain
    // substring/prefix matching against the literal name and domain.
    private const int ExactNameScore = 100;
    private const int ExactDomainScore = 90;
    private const int NameStartsWithScore = 80;
    private const int DomainStartsWithScore = 70;
    private const int NameContainsScore = 60;
    private const int DomainContainsScore = 50;

    private readonly ICompanyValidator _validator;
    private readonly ICompanyRelevanceEvaluator _relevanceEvaluator;
    private readonly ICompanyRepository _repository;

    public CompanyService(
        ICompanyValidator validator,
        ICompanyRelevanceEvaluator relevanceEvaluator,
        ICompanyRepository repository)
    {
        _validator = validator;
        _relevanceEvaluator = relevanceEvaluator;
        _repository = repository;
    }

    public async Task<CreateCompanyResult> CreateCompanyAsync(
        string? name,
        string? websiteUrl,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(name, websiteUrl);
        if (!validationResult.IsValid)
        {
            return CreateCompanyResult.ValidationFailure(validationResult.Errors);
        }

        var relevanceResult = _relevanceEvaluator.Evaluate(name, websiteUrl);
        if (!relevanceResult.IsRelevant)
        {
            return CreateCompanyResult.RelevanceFailure(NotRelevantMessage);
        }

        var company = new Company(Guid.NewGuid(), name!, websiteUrl!);
        await _repository.AddAsync(company, cancellationToken);

        return CreateCompanyResult.Success(company);
    }

    public Task<IReadOnlyList<Company>> GetAllCompaniesAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<Company>> SearchCompaniesAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var companies = await _repository.GetAllAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(query))
        {
            return companies;
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        return companies
            .Select(company => new CompanySearchResult(company, ScoreCompany(company, normalizedQuery)))
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Company.Name, StringComparer.OrdinalIgnoreCase)
            .Select(result => result.Company)
            .ToList();
    }

    private static int ScoreCompany(Company company, string normalizedQuery)
    {
        var normalizedName = company.Name.ToLowerInvariant();
        var normalizedDomain = ExtractNormalizedDomain(company.WebsiteUrl);

        var nameScore = ScoreField(normalizedName, normalizedQuery, ExactNameScore, NameStartsWithScore, NameContainsScore);
        var domainScore = normalizedDomain is null
            ? 0
            : ScoreField(normalizedDomain, normalizedQuery, ExactDomainScore, DomainStartsWithScore, DomainContainsScore);

        return Math.Max(nameScore, domainScore);
    }

    private static int ScoreField(
        string normalizedValue,
        string normalizedQuery,
        int exactScore,
        int startsWithScore,
        int containsScore)
    {
        if (normalizedValue == normalizedQuery)
        {
            return exactScore;
        }

        if (normalizedValue.StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            return startsWithScore;
        }

        if (normalizedValue.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return containsScore;
        }

        return 0;
    }

    private static string? ExtractNormalizedDomain(string websiteUrl)
    {
        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();

        return host.StartsWith("www.", StringComparison.Ordinal)
            ? host["www.".Length..]
            : host;
    }

    private sealed record CompanySearchResult(Company Company, int Score);
}
