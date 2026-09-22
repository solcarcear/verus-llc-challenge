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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchCompaniesAsync_NullOrEmptyQuery_ReturnsAllCompanies(string? query)
    {
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com"));
        repository.Companies.Add(new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com"));
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync(query);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchCompaniesAsync_ExactCompanyNameMatch_ReturnsCompany()
    {
        var company = new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(company);
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("Microsoft Corporation");

        Assert.Single(results);
        Assert.Equal(company, results[0]);
    }

    [Fact]
    public async Task SearchCompaniesAsync_PartialCompanyNameMatch_ReturnsCompany()
    {
        var company = new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(company);
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("micro");

        Assert.Single(results);
        Assert.Equal(company, results[0]);
    }

    [Fact]
    public async Task SearchCompaniesAsync_CompanyNameMatch_IsCaseInsensitive()
    {
        var company = new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(company);
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("MICROSOFT");

        Assert.Single(results);
        Assert.Equal(company, results[0]);
    }

    [Fact]
    public async Task SearchCompaniesAsync_ExactDomainMatch_ReturnsCompany()
    {
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(company);
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("acme.com");

        Assert.Single(results);
        Assert.Equal(company, results[0]);
    }

    [Fact]
    public async Task SearchCompaniesAsync_PartialDomainMatch_ReturnsCompany()
    {
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(company);
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("acme");

        Assert.Single(results);
        Assert.Equal(company, results[0]);
    }

    [Fact]
    public async Task SearchCompaniesAsync_IgnoresWwwPrefix_WhenMatchingDomain()
    {
        var company = new Company(Guid.NewGuid(), "Microsoft Corporation", "https://www.microsoft.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(company);
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("microsoft.com");

        Assert.Single(results);
        Assert.Equal(company, results[0]);
    }

    [Fact]
    public async Task SearchCompaniesAsync_ExcludesUnrelatedCompanies()
    {
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com"));
        repository.Companies.Add(new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com"));
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("micro");

        Assert.Single(results);
        Assert.Equal("Microsoft Corporation", results[0].Name);
    }

    [Fact]
    public async Task SearchCompaniesAsync_OrdersStrongestMatchesFirst()
    {
        var exactMatch = new Company(Guid.NewGuid(), "Acme", "https://acme.com");
        var containsMatch = new Company(Guid.NewGuid(), "Global Acme Partners", "https://globalacme.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(containsMatch);
        repository.Companies.Add(exactMatch);
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("acme");

        Assert.Equal(2, results.Count);
        Assert.Equal(exactMatch, results[0]);
        Assert.Equal(containsMatch, results[1]);
    }

    [Fact]
    public async Task SearchCompaniesAsync_UsesAlphabeticalOrder_WhenScoresTie()
    {
        var acmeGroup = new Company(Guid.NewGuid(), "Acme Group", "https://acme-group.com");
        var acmeCorp = new Company(Guid.NewGuid(), "Acme Corp", "https://acme-corp.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(acmeGroup);
        repository.Companies.Add(acmeCorp);
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("acme");

        Assert.Equal(2, results.Count);
        Assert.Equal(acmeCorp, results[0]);
        Assert.Equal(acmeGroup, results[1]);
    }

    [Fact]
    public async Task SearchCompaniesAsync_NoMatches_ReturnsEmptyCollection()
    {
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com"));
        var service = CreateService(repository);

        var results = await service.SearchCompaniesAsync("nonexistent");

        Assert.Empty(results);
    }

    [Fact]
    public async Task UpdateCompanyAsync_ExistingCompany_UpdatesAndReturnsIt()
    {
        var existing = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(existing);
        var service = CreateService(repository);

        var result = await service.UpdateCompanyAsync(existing.Id, "Acme Global", "https://acmeglobal.com");

        Assert.True(result.IsSuccess);
        Assert.Equal(CompanyUpdateStatus.Updated, result.Status);
        Assert.Equal("Acme Global", result.Company!.Name);
        Assert.Equal("https://acmeglobal.com", result.Company.WebsiteUrl);
        Assert.Single(repository.Companies);
        Assert.Equal("Acme Global", repository.Companies[0].Name);
    }

    [Fact]
    public async Task UpdateCompanyAsync_UnknownId_ReturnsNotFound_WithoutValidatingOrPersisting()
    {
        var validator = new StubCompanyValidator(CompanyValidationResult.Success());
        var repository = new FakeCompanyRepository();
        var service = new CompanyService(validator, new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100)), repository);

        var result = await service.UpdateCompanyAsync(Guid.NewGuid(), "Acme Global", "https://acmeglobal.com");

        Assert.False(result.IsSuccess);
        Assert.Equal(CompanyUpdateStatus.NotFound, result.Status);
        Assert.False(validator.WasCalled);
    }

    [Fact]
    public async Task UpdateCompanyAsync_InvalidInput_ReturnsValidationFailure_AndDoesNotPersist()
    {
        var existing = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(existing);
        var validationErrors = new[] { "Company name is required." };
        var validator = new StubCompanyValidator(CompanyValidationResult.Failure(validationErrors));
        var service = new CompanyService(validator, new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100)), repository);

        var result = await service.UpdateCompanyAsync(existing.Id, "", "not-a-url");

        Assert.False(result.IsSuccess);
        Assert.Equal(CompanyUpdateStatus.ValidationFailed, result.Status);
        Assert.Equal(validationErrors, result.Errors);
        Assert.Equal("Acme Corp", repository.Companies[0].Name);
    }

    [Fact]
    public async Task UpdateCompanyAsync_IrrelevantNameAndWebsite_ReturnsNotRelevant_AndDoesNotPersist()
    {
        var existing = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(existing);
        var validator = new StubCompanyValidator(CompanyValidationResult.Success());
        var relevanceEvaluator = new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.NotRelevant());
        var service = new CompanyService(validator, relevanceEvaluator, repository);

        var result = await service.UpdateCompanyAsync(existing.Id, "Microsoft Corporation", "https://apple.com");

        Assert.False(result.IsSuccess);
        Assert.Equal(CompanyUpdateStatus.NotRelevant, result.Status);
        Assert.Equal("Acme Corp", repository.Companies[0].Name);
    }

    [Fact]
    public async Task DeleteCompanyAsync_ExistingCompany_RemovesItAndReturnsTrue()
    {
        var existing = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var repository = new FakeCompanyRepository();
        repository.Companies.Add(existing);
        var service = CreateService(repository);

        var deleted = await service.DeleteCompanyAsync(existing.Id);

        Assert.True(deleted);
        Assert.Empty(repository.Companies);
    }

    [Fact]
    public async Task DeleteCompanyAsync_UnknownId_ReturnsFalse()
    {
        var service = CreateService(new FakeCompanyRepository());

        var deleted = await service.DeleteCompanyAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    private static CompanyService CreateService(ICompanyRepository repository) =>
        new(
            new StubCompanyValidator(CompanyValidationResult.Success()),
            new StubCompanyRelevanceEvaluator(CompanyRelevanceResult.Relevant(100)),
            repository);

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

        public Task<(IReadOnlyList<Company> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var ordered = Companies.OrderBy(c => c.Name, StringComparer.Ordinal).ThenBy(c => c.Id).ToList();
            IReadOnlyList<Company> page = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return Task.FromResult((page, ordered.Count));
        }

        public Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
        {
            var index = Companies.FindIndex(c => c.Id == company.Id);
            if (index >= 0)
            {
                Companies[index] = company;
            }

            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Companies.RemoveAll(c => c.Id == id) > 0);

        public Task<(IReadOnlyList<CompanySummary> Items, int TotalCount)> GetPagedSummariesAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var ordered = Companies.OrderBy(c => c.Name, StringComparer.Ordinal).ThenBy(c => c.Id).ToList();
            IReadOnlyList<CompanySummary> page = ordered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CompanySummary(c.Id, c.Name, c.WebsiteUrl, c.Contacts.Count, c.Orders.Count))
                .ToList();

            return Task.FromResult((page, ordered.Count));
        }

        public Task<Company?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Companies.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyDictionary<Guid, CompanyRelationshipCounts>> GetRelationshipCountsAsync(
            IReadOnlyCollection<Guid> companyIds,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, CompanyRelationshipCounts> result = companyIds.ToDictionary(
                id => id,
                id =>
                {
                    var company = Companies.FirstOrDefault(c => c.Id == id);
                    return new CompanyRelationshipCounts(company?.Contacts.Count ?? 0, company?.Orders.Count ?? 0);
                });

            return Task.FromResult(result);
        }
    }
}
