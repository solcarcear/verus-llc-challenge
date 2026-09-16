using CompanyManagement.Domain;
using CompanyManagement.Infrastructure.Persistence;
using Xunit;

namespace CompanyManagement.UnitTests.Persistence;

public class InMemoryCompanyRepositoryTests
{
    private readonly InMemoryCompanyRepository _repository = new();

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheSameCompany()
    {
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");

        await _repository.AddAsync(company);
        var retrieved = await _repository.GetByIdAsync(company.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(company.Id, retrieved!.Id);
        Assert.Equal(company.Name, retrieved.Name);
        Assert.Equal(company.WebsiteUrl, retrieved.WebsiteUrl);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyCollection_WhenNoCompaniesExist()
    {
        var companies = await _repository.GetAllAsync();

        Assert.NotNull(companies);
        Assert.Empty(companies);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAddedCompanies_StoredIndependently()
    {
        var first = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var second = new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com");

        await _repository.AddAsync(first);
        await _repository.AddAsync(second);

        var companies = await _repository.GetAllAsync();

        Assert.Equal(2, companies.Count);
        Assert.Contains(companies, c => c.Id == first.Id && c.Name == first.Name);
        Assert.Contains(companies, c => c.Id == second.Id && c.Name == second.Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSnapshot_NotLiveViewOfInternalStorage()
    {
        var first = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        await _repository.AddAsync(first);

        var snapshot = await _repository.GetAllAsync();

        var second = new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com");
        await _repository.AddAsync(second);

        Assert.Single(snapshot);
        Assert.DoesNotContain(snapshot, c => c.Id == second.Id);
    }

    [Fact]
    public async Task AddAsync_ConcurrentAdds_AllCompaniesAreStoredWithoutLoss()
    {
        var companies = Enumerable.Range(0, 100)
            .Select(i => new Company(Guid.NewGuid(), $"Company {i}", $"https://company{i}.com"))
            .ToList();

        await Task.WhenAll(companies.Select(company => _repository.AddAsync(company)));

        var stored = await _repository.GetAllAsync();

        Assert.Equal(companies.Count, stored.Count);
        foreach (var company in companies)
        {
            Assert.Contains(stored, c => c.Id == company.Id);
        }
    }
}
