using CompanyManagement.Application.Persistence;
using CompanyManagement.Application.Relevance;
using CompanyManagement.Application.Validation;
using CompanyManagement.Domain;

namespace CompanyManagement.Application.Services;

public sealed class CompanyService : ICompanyService
{
    private const string NotRelevantMessage = "Company name is not sufficiently relevant to the provided website.";

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
}
