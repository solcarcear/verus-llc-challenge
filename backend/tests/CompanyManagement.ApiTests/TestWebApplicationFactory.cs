using CompanyManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyManagement.ApiTests;

/// <summary>
/// Swaps the real SQL Server-backed <see cref="CompanyManagementDbContext"/> for the EF Core
/// InMemory provider, so integration tests don't require a running SQL Server instance.
/// Each factory instance gets its own database name to keep tests isolated from each other.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"CompanyManagementTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CompanyManagementDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<CompanyManagementDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
