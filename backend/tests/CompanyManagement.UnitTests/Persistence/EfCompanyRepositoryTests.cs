using CompanyManagement.Domain;
using CompanyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompanyManagement.UnitTests.Persistence;

public class EfCompanyRepositoryTests
{
    private static EfCompanyRepository CreateRepository(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<CompanyManagementDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new EfCompanyRepository(new CompanyManagementDbContext(options));
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheSameCompany()
    {
        var repository = CreateRepository();
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");

        await repository.AddAsync(company);
        var retrieved = await repository.GetByIdAsync(company.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(company.Id, retrieved!.Id);
        Assert.Equal(company.Name, retrieved.Name);
        Assert.Equal(company.WebsiteUrl, retrieved.WebsiteUrl);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyCollection_WhenNoCompaniesExist()
    {
        var repository = CreateRepository();

        var companies = await repository.GetAllAsync();

        Assert.NotNull(companies);
        Assert.Empty(companies);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var repository = CreateRepository();

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAddedCompanies()
    {
        var repository = CreateRepository();
        var first = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var second = new Company(Guid.NewGuid(), "Microsoft Corporation", "https://microsoft.com");

        await repository.AddAsync(first);
        await repository.AddAsync(second);

        var companies = await repository.GetAllAsync();

        Assert.Equal(2, companies.Count);
        Assert.Contains(companies, c => c.Id == first.Id && c.Name == first.Name);
        Assert.Contains(companies, c => c.Id == second.Id && c.Name == second.Name);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsRequestedPageSize_AndCorrectTotalCount()
    {
        var repository = CreateRepository();
        foreach (var company in BuildCompanies(25))
        {
            await repository.AddAsync(company);
        }

        var (items, totalCount) = await repository.GetPagedAsync(pageNumber: 1, pageSize: 10);

        Assert.Equal(10, items.Count);
        Assert.Equal(25, totalCount);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsDifferentRecordsThanFirstPage()
    {
        var repository = CreateRepository();
        foreach (var company in BuildCompanies(25))
        {
            await repository.AddAsync(company);
        }

        var (firstPage, _) = await repository.GetPagedAsync(pageNumber: 1, pageSize: 10);
        var (secondPage, _) = await repository.GetPagedAsync(pageNumber: 2, pageSize: 10);

        var firstPageIds = firstPage.Select(c => c.Id).ToHashSet();
        Assert.DoesNotContain(secondPage, c => firstPageIds.Contains(c.Id));
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByNameThenById_Deterministically()
    {
        var repository = CreateRepository();
        var companies = BuildCompanies(5);
        // Add in reverse to prove ordering comes from the query, not insertion order.
        foreach (var company in companies.AsEnumerable().Reverse())
        {
            await repository.AddAsync(company);
        }

        var (firstCall, _) = await repository.GetPagedAsync(pageNumber: 1, pageSize: 5);
        var (secondCall, _) = await repository.GetPagedAsync(pageNumber: 1, pageSize: 5);

        Assert.Equal(firstCall.Select(c => c.Id), secondCall.Select(c => c.Id));
        Assert.Equal(companies.OrderBy(c => c.Name, StringComparer.Ordinal).Select(c => c.Id), firstCall.Select(c => c.Id));
    }

    [Fact]
    public async Task GetPagedAsync_PageBeyondTotalCount_ReturnsEmptyItems_ButStillReportsTotalCount()
    {
        var repository = CreateRepository();
        foreach (var company in BuildCompanies(3))
        {
            await repository.AddAsync(company);
        }

        var (items, totalCount) = await repository.GetPagedAsync(pageNumber: 5, pageSize: 10);

        Assert.Empty(items);
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task UpdateAsync_ExistingCompany_PersistsTheChanges()
    {
        // A separate repository (fresh DbContext) per call mirrors production, where
        // every HTTP request gets its own scoped DbContext - and avoids EF's "already
        // tracked with a different instance" conflict that reusing one DbContext here
        // would otherwise hit, since Company has no setters to mutate a tracked instance.
        var databaseName = Guid.NewGuid().ToString();
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        await CreateRepository(databaseName).AddAsync(company);

        var updated = new Company(company.Id, "Acme Global", "https://acmeglobal.com");
        await CreateRepository(databaseName).UpdateAsync(updated);

        var retrieved = await CreateRepository(databaseName).GetByIdAsync(company.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Acme Global", retrieved!.Name);
        Assert.Equal("https://acmeglobal.com", retrieved.WebsiteUrl);
    }

    [Fact]
    public async Task DeleteAsync_ExistingCompany_RemovesItAndReturnsTrue()
    {
        var databaseName = Guid.NewGuid().ToString();
        var company = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        await CreateRepository(databaseName).AddAsync(company);

        var deleted = await CreateRepository(databaseName).DeleteAsync(company.Id);

        Assert.True(deleted);
        var retrieved = await CreateRepository(databaseName).GetByIdAsync(company.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var deleted = await CreateRepository().DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    private static List<Company> BuildCompanies(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new Company(Guid.NewGuid(), $"Company {i:D2}", $"https://company{i}.com"))
            .ToList();
}
