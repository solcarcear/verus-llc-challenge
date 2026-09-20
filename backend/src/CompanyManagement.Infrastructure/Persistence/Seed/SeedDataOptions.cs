namespace CompanyManagement.Infrastructure.Persistence.Seed;

public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; set; }

    public int CompanyCount { get; set; } = 5000;
}
