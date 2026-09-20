using CompanyManagement.Domain;
using CompanyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompanyManagement.UnitTests.Persistence;

public class EfCompanyRepositoryTests
{
    private static EfCompanyRepository CreateRepository()
    {
        var options = new DbContextOptionsBuilder<CompanyManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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
}
