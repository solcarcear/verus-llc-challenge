using CompanyManagement.Domain;
using CompanyManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace CompanyManagement.UnitTests.Persistence.Seed;

public class OrderSeedDataGeneratorTests
{
    [Fact]
    public void Generate_IsDeterministic_AcrossRepeatedCalls()
    {
        var companies = CompanySeedDataGenerator.Generate(200);

        var first = OrderSeedDataGenerator.Generate(companies);
        var second = OrderSeedDataGenerator.Generate(companies);

        Assert.Equal(
            first.Select(o => (o.Id, o.CompanyId, o.OrderNumber, o.Amount, o.Status)),
            second.Select(o => (o.Id, o.CompanyId, o.OrderNumber, o.Amount, o.Status)));
    }

    [Fact]
    public void Generate_EveryOrder_HasAValidCompanyId()
    {
        var companies = CompanySeedDataGenerator.Generate(200);
        var companyIds = companies.Select(c => c.Id).ToHashSet();

        var orders = OrderSeedDataGenerator.Generate(companies);

        Assert.All(orders, order => Assert.Contains(order.CompanyId, companyIds));
    }

    [Fact]
    public void Generate_ProducesUniqueOrderNumbers_AcrossTheWholeDataset()
    {
        var companies = CompanySeedDataGenerator.Generate(500);

        var orders = OrderSeedDataGenerator.Generate(companies);

        Assert.Equal(orders.Count, orders.Select(o => o.OrderNumber).Distinct().Count());
    }

    [Fact]
    public void Generate_ProducesUniqueDeterministicIds()
    {
        var companies = CompanySeedDataGenerator.Generate(500);

        var orders = OrderSeedDataGenerator.Generate(companies);

        Assert.Equal(orders.Count, orders.Select(o => o.Id).Distinct().Count());
        Assert.All(orders, order => Assert.StartsWith("4f524452-0000-0000-", order.Id.ToString()));
    }

    [Fact]
    public void Generate_VariesOrderCountPerCompany_IncludingSomeWithZero()
    {
        var companies = CompanySeedDataGenerator.Generate(500);

        var orders = OrderSeedDataGenerator.Generate(companies);
        var countsByCompany = orders.GroupBy(o => o.CompanyId).ToDictionary(g => g.Key, g => g.Count());

        Assert.True(countsByCompany.Values.Distinct().Count() > 1, "Expected a variable number of orders per company.");
        Assert.True(companies.Count > countsByCompany.Count, "Expected at least one company with zero orders.");
    }

    [Fact]
    public void Generate_ProducesEveryOrderStatus()
    {
        var companies = CompanySeedDataGenerator.Generate(2000);

        var orders = OrderSeedDataGenerator.Generate(companies);
        var statuses = orders.Select(o => o.Status).Distinct().ToHashSet();

        Assert.Equal(Enum.GetValues<OrderStatus>().Length, statuses.Count);
    }

    [Fact]
    public void Generate_PendingOrders_HaveNoUpdatedAt_OthersDo()
    {
        var companies = CompanySeedDataGenerator.Generate(2000);

        var orders = OrderSeedDataGenerator.Generate(companies);

        Assert.All(orders.Where(o => o.Status == OrderStatus.Pending), o => Assert.Null(o.UpdatedAt));
        Assert.Contains(orders, o => o.Status != OrderStatus.Pending && o.UpdatedAt is not null);
    }

    [Fact]
    public void Generate_AllAmounts_ArePositive()
    {
        var companies = CompanySeedDataGenerator.Generate(200);

        var orders = OrderSeedDataGenerator.Generate(companies);

        Assert.All(orders, order => Assert.True(order.Amount > 0));
    }
}
