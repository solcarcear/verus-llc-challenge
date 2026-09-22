using CompanyManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace CompanyManagement.Infrastructure.Persistence;

public sealed class CompanyManagementDbContext : DbContext
{
    public CompanyManagementDbContext(DbContextOptions<CompanyManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(company =>
        {
            company.ToTable("Companies");
            company.HasKey(c => c.Id);
            company.Property(c => c.Name).IsRequired().HasMaxLength(200);
            company.Property(c => c.WebsiteUrl).IsRequired().HasMaxLength(2048);

            // Matches the OrderBy(Name).ThenBy(Id) used for paginated listing, so SQL
            // Server can satisfy that ORDER BY directly from the index instead of
            // sorting the whole table on every page request.
            company.HasIndex(c => new { c.Name, c.Id }).HasDatabaseName("IX_Companies_Name_Id");
        });
    }
}
