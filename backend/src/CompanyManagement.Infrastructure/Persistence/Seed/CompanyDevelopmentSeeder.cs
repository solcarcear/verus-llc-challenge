using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompanyManagement.Infrastructure.Persistence.Seed;

// Development-only bootstrap data. Deliberately bypasses ICompanyRepository
// and writes through the DbContext directly so the ~5,000 rows can be
// inserted in a handful of batches instead of one round trip per company.
public sealed class CompanyDevelopmentSeeder
{
    private const int BatchSize = 500;

    private readonly CompanyManagementDbContext _dbContext;
    private readonly ILogger<CompanyDevelopmentSeeder> _logger;

    public CompanyDevelopmentSeeder(CompanyManagementDbContext dbContext, ILogger<CompanyDevelopmentSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(int companyCount, CancellationToken cancellationToken = default)
    {
        if (companyCount <= 0)
        {
            return;
        }

        // Idempotency check: look for the first seed record's deterministic Id
        // rather than Companies.Any(), so companies a developer already
        // created by hand before running the seeder don't cause it to skip.
        var markerId = CompanySeedDataGenerator.SeedId(0);
        var alreadySeeded = await _dbContext.Companies
            .AsNoTracking()
            .AnyAsync(c => c.Id == markerId, cancellationToken);

        if (alreadySeeded)
        {
            _logger.LogInformation("Development seed data already present; skipping company seeding.");
            return;
        }

        var companies = CompanySeedDataGenerator.Generate(companyCount);

        foreach (var batch in companies.Chunk(BatchSize))
        {
            await _dbContext.Companies.AddRangeAsync(batch, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Seeded {Count} development companies.", companies.Count);
    }
}
