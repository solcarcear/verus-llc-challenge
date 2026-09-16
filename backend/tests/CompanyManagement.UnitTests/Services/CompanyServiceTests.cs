using CompanyManagement.Application.Persistence;
using CompanyManagement.Application.Relevance;
using CompanyManagement.Application.Services;
using CompanyManagement.Application.Validation;
using CompanyManagement.Domain;
using Xunit;

namespace CompanyManagement.UnitTests.Services;

public class CompanyServiceTests
{
    [Fact]
    public async Task CreateCompanyAsync_ValidAndRelevantCompany_IsPersistedAndReturned()
    {
        var validator = new StubCompanyValidator(CompanyValidationResult.Success());
        var relevanceEvaluator = new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100));
        var repository = new FakeCompanyRepository();
        var service = new CompanyService(validator, relevanceEvaluator, repository);

        var result = await service.CreateCompanyAsync("Acme Corp", "https://acme.com");

        Assert.True(result.IsSuccess);
        Assert.Equal(CompanyCreationStatus.Created, result.Status);
        Assert.NotNull(result.Company);
        Assert.Equal("Acme Corp", result.Company!.Name);
        Assert.Equal("https://acme.com", result.Company.WebsiteUrl);
        Assert.Single(repository.Companies);
        Assert.Equal(result.Company, repository.Companies[0]);
    }

    [Fact]
    public async Task CreateCompanyAsync_GeneratesNonEmptyGuid_ForNewCompany()
    {
        var validator = new StubCompanyValidator(CompanyValidationResult.Success());
        var relevanceEvaluator = new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100));
        var service = new CompanyService(validator, relevanceEvaluator, new FakeCompanyRepository());

        var result = await service.CreateCompanyAsync("Acme Corp", "https://acme.com");

        Assert.NotNull(result.Company);
        Assert.NotEqual(Guid.Empty, result.Company!.Id);
    }

    [Fact]
    public async Task CreateCompanyAsync_ValidationFailure_PreventsRelevanceCheckAndPersistence()
    {
        var validationErrors = new[] { "Company name is required." };
        var validator = new StubCompanyValidator(CompanyValidationResult.Failure(validationErrors));
        var relevanceEvaluator = new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100));
        var repository = new FakeCompanyRepository();
        var service = new CompanyService(validator, relevanceEvaluator, repository);

        var result = await service.CreateCompanyAsync(null, "https://acme.com");

        Assert.False(result.IsSuccess);
        Assert.Equal(CompanyCreationStatus.ValidationFailed, result.Status);
        Assert.Null(result.Company);
        Assert.Equal(validationErrors, result.Errors);
        Assert.False(relevanceEvaluator.WasCalled);
        Assert.Empty(repository.Companies);
    }

    [Fact]
    public async Task CreateCompanyAsync_RelevanceFailure_PreventsPersistence()
    {
        var validator = new StubCompanyValidator(CompanyValidationResult.Success());
        var relevanceEvaluator = new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.NotRelevant());
        var repository = new FakeCompanyRepository();
        var service = new CompanyService(validator, relevanceEvaluator, repository);

        var result = await service.CreateCompanyAsync("Microsoft Corporation", "https://apple.com");

        Assert.False(result.IsSuccess);
        Assert.Equal(CompanyCreationStatus.NotRelevant, result.Status);
        Assert.Null(result.Company);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(repository.Companies);
    }

    [Fact]
    public async Task CreateCompanyAsync_RelevantCompany_IsSuccessfullyCreated()
    {
        var validator = new StubCompanyValidator(CompanyValidationResult.Success());
        var relevanceEvaluator = new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(80));
        var repository = new FakeCompanyRepository();
        var service = new CompanyService(validator, relevanceEvaluator, repository);

        var result = await service.CreateCompanyAsync("Acme Global", "https://acme.com");

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Companies);
    }

    [Fact]
    public async Task GetAllCompaniesAsync_ReturnsRepositoryData()
    {
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com"));
        repository.Companies.Add(new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com"));
        var service = new CompanyService(
            new StubCompanyValidator(CompanyValidationResult.Success()),
            new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100)),
            repository);

        var companies = await service.GetAllCompaniesAsync();

        Assert.Equal(repository.Companies, companies);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_ReturnsExistingCompany()
    {
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(company);
        var service = new CompanyService(
            new StubCompanyValidator(CompanyValidationResult.Success()),
            new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100)),
            repository);

        var result = await service.GetCompanyByIdAsync(company.Id);

        Assert.Equal(company, result);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_ReturnsNull_ForUnknownId()
    {
        var service = new CompanyService(
            new StubCompanyValidator(CompanyValidationResult.Success()),
            new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100)),
            new FakeCompanyRepository());

        var result = await service.GetCompanyByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    private sealed class StubCompanyValidator : ICompanyValidator
    {
        private readonly CompanyValidationResult _result;

        public StubCompanyValidator(CompanyValidationResult result) => _result = result;

        public bool WasCalled { get; private set; }

        public CompanyValidationResult Validate(string? name, string? websiteUrl)
        {
            WasCalled = true;
            return _result;
        }
    }

    private sealed class StubCompanyRelevanceEvaluator : ICompanyRelevanceEvaluator
    {
        private readonly CompanyRelevanceResult _result;

        public StubCompanyRelevanceEvaluator(CompanyRelevanceResult result) => _result = result;

        public bool WasCalled { get; private set; }

        public CompanyRelevanceResult Evaluate(string? companyName, string? websiteUrl)
        {
            WasCalled = true;
            return _result;
        }
    }

    private sealed class FakeCompanyRepository : ICompanyRepository
    {
        public List<Company> Companies { get; } = new();

        public Task AddAsync(Company company, CancellationToken cancellationToken = default)
        {
            Companies.Add(company);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Company>>(Companies.ToList());

        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Companies.FirstOrDefault(c => c.Id == id));
    }
}
