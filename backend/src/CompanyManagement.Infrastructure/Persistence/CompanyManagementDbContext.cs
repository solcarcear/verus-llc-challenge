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
        });
    }
}
