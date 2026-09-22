using CompanyManagement.Domain;

namespace CompanyManagement.Infrastructure.Persistence.Seed;

// Deterministic, like CompanySeedDataGenerator and ContactSeedDataGenerator.
// Order counts per company are randomized between 0 and
// MaxOrdersPerCompany (inclusive), so a meaningful fraction of companies end
// up with zero orders - intentional, for LEFT JOIN / NOT EXISTS practice.
public static class OrderSeedDataGenerator
{
    // ASCII "ORDR" as the Guid's first component, mirroring the other
    // generators' Id-prefix markers.
    private const int SeedIdMagic = 0x4F524452;

    private const int MinOrdersPerCompany = 0;
    private const int MaxOrdersPerCompany = 30;
    private const int LookbackDays = 730;
    private const double MinAmount = 50;
    private const double MaxAmount = 25_000;

    public static Guid SeedId(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new Guid(SeedIdMagic, 0, 0, BitConverter.GetBytes((long)index));
    }

    public static IReadOnlyList<Order> Generate(IReadOnlyList<Company> companies)
    {
        var orders = new List<Order>();
        var now = DateTime.UtcNow;
        var globalIndex = 0;

        for (var companyIndex = 0; companyIndex < companies.Count; companyIndex++)
        {
            var company = companies[companyIndex];
            var rng = new Random(unchecked(SeedIdMagic + (companyIndex * 15485863)));
            var orderCount = rng.Next(MinOrdersPerCompany, MaxOrdersPerCompany + 1);

            for (var i = 0; i < orderCount; i++)
            {
                var status = PickStatus(rng);
                var amount = (decimal)Math.Round(rng.NextDouble() * (MaxAmount - MinAmount) + MinAmount, 2);
                var createdAt = now.AddDays(-rng.Next(0, LookbackDays)).AddSeconds(-rng.Next(0, 86_400));

                // Pending orders haven't been touched since creation; everything
                // else has moved forward at least once, sometime after CreatedAt
                // and never later than "now".
                DateTime? updatedAt = status == OrderStatus.Pending
                    ? null
                    : MinDate(createdAt.AddDays(rng.Next(1, 30)), now);

                // A strictly incrementing counter, not random text, so OrderNumber
                // stays globally unique without needing a collision check -
                // required since Orders.OrderNumber is unique.
                var orderNumber = $"ORD-{globalIndex:D8}";

                orders.Add(new Order(
                    SeedId(globalIndex),
                    company.Id,
                    orderNumber,
                    amount,
                    status,
                    createdAt,
                    updatedAt));

                globalIndex++;
            }
        }

        return orders;
    }

    // Skews toward Completed, since that's what a mature order history
    // realistically looks like, while still exercising every status value.
    private static OrderStatus PickStatus(Random rng)
    {
        var roll = rng.Next(100);
        return roll switch
        {
            < 15 => OrderStatus.Pending,
            < 30 => OrderStatus.Processing,
            < 90 => OrderStatus.Completed,
            _ => OrderStatus.Cancelled,
        };
    }

    private static DateTime MinDate(DateTime a, DateTime b) => a < b ? a : b;
}
