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

    [Fact]
    public async Task UpdateAsync_ExistingCompany_ReplacesItInStorage()
    {
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        await _repository.AddAsync(company);

        var updated = new Company(company.Id, "Acme Global", "https://acmeglobal.com");
        await _repository.UpdateAsync(updated);

        var retrieved = await _repository.GetByIdAsync(company.Id);
        Assert.Equal("Acme Global", retrieved!.Name);
        Assert.Equal("https://acmeglobal.com", retrieved.WebsiteUrl);
    }

    [Fact]
    public async Task DeleteAsync_ExistingCompany_RemovesItAndReturnsTrue()
    {
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        await _repository.AddAsync(company);

        var deleted = await _repository.DeleteAsync(company.Id);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(company.Id));
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var deleted = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsRequestedPageSize_AndCorrectTotalCount()
    {
        foreach (var company in Enumerable.Range(0, 25).Select(i => new Company(Guid.NewGuid(), $"Company {i:D2}", $"https://company{i}.com")))
        {
            await _repository.AddAsync(company);
        }

        var (items, totalCount) = await _repository.GetPagedAsync(pageNumber: 2, pageSize: 10);

        Assert.Equal(10, items.Count);
        Assert.Equal(25, totalCount);
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByNameThenById_Deterministically()
    {
        var first = new Company(Guid.NewGuid(), "Bravo", "https://bravo.com");
        var second = new Company(Guid.NewGuid(), "Alpha", "https://alpha.com");
        await _repository.AddAsync(first);
        await _repository.AddAsync(second);

        var (items, _) = await _repository.GetPagedAsync(pageNumber: 1, pageSize: 10);

        Assert.Equal(new[] { second.Id, first.Id }, items.Select(c => c.Id));
    }
}
