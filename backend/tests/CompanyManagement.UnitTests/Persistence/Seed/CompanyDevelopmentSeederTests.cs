using CompanyManagement.Domain;
using CompanyManagement.Infrastructure.Persistence;
using CompanyManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CompanyManagement.UnitTests.Persistence.Seed;

public class CompanyDevelopmentSeederTests
{
    private const int SmallSeedCount = 25;

    private static CompanyManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CompanyManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CompanyManagementDbContext(options);
    }

    private static CompanyDevelopmentSeeder CreateSeeder(CompanyManagementDbContext dbContext) =>
        new(dbContext, NullLogger<CompanyDevelopmentSeeder>.Instance);

    [Fact]
    public async Task SeedAsync_PopulatesTheRequestedNumberOfCompanies()
    {
        await using var dbContext = CreateDbContext();
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync(SmallSeedCount);

        Assert.Equal(SmallSeedCount, await dbContext.Companies.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_DoesNotDuplicateRecords()
    {
        await using var dbContext = CreateDbContext();
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync(SmallSeedCount);
        await seeder.SeedAsync(SmallSeedCount);
        await seeder.SeedAsync(SmallSeedCount);

        Assert.Equal(SmallSeedCount, await dbContext.Companies.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_DoesNotDeleteOrOverwriteExistingUserCreatedCompanies()
    {
        await using var dbContext = CreateDbContext();
        var existingCompany = new Company(Guid.NewGuid(), "Hand Crafted Widgets", "https://handcraftedwidgets.com");
        dbContext.Companies.Add(existingCompany);
        await dbContext.SaveChangesAsync();

        var seeder = CreateSeeder(dbContext);
        await seeder.SeedAsync(SmallSeedCount);

        Assert.Equal(SmallSeedCount + 1, await dbContext.Companies.CountAsync());

        var stillPresent = await dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == existingCompany.Id);
        Assert.NotNull(stillPresent);
        Assert.Equal(existingCompany.Name, stillPresent!.Name);
        Assert.Equal(existingCompany.WebsiteUrl, stillPresent.WebsiteUrl);
    }

    [Fact]
    public async Task SeedAsync_WithNonPositiveCount_DoesNothing()
    {
        await using var dbContext = CreateDbContext();
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync(0);

        Assert.Equal(0, await dbContext.Companies.CountAsync());
    }
}
