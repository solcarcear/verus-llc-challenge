using System.Net;
using System.Net.Http.Json;
using CompanyManagement.Api.Contracts;
using CompanyManagement.Domain;
using CompanyManagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyManagement.ApiTests;

public class OrdersControllerTests
{
    [Fact]
    public async Task GetOrders_WhenNoneExist_ReturnsEmptyPage()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task GetOrders_FiltersByCompanyId()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();
        await CompaniesControllerTests.SeedContactsAndOrdersAsync(factory, companyAId, contactCount: 0, orderCount: 3);
        await CompaniesControllerTests.SeedContactsAndOrdersAsync(factory, companyBId, contactCount: 0, orderCount: 1);

        var response = await client.GetAsync($"/api/orders?companyId={companyAId}");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.Equal(3, page!.Items.Count);
        Assert.All(page.Items, o => Assert.Equal(companyAId, o.CompanyId));
    }

    [Fact]
    public async Task GetOrders_FiltersByStatus()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var companyId = Guid.NewGuid();
        await using (var dbContext = CreateDbContext(factory))
        {
            dbContext.Orders.Add(new Order(Guid.NewGuid(), companyId, "ORD-1", 100m, OrderStatus.Completed, DateTime.UtcNow, null));
            dbContext.Orders.Add(new Order(Guid.NewGuid(), companyId, "ORD-2", 200m, OrderStatus.Pending, DateTime.UtcNow, null));
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/orders?status=Completed");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        var item = Assert.Single(page!.Items);
        Assert.Equal("Completed", item.Status);
    }

    [Fact]
    public async Task GetOrders_UnrecognizedStatus_IsTreatedAsNoFilter()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var companyId = Guid.NewGuid();
        await CompaniesControllerTests.SeedContactsAndOrdersAsync(factory, companyId, contactCount: 0, orderCount: 2);

        var response = await client.GetAsync("/api/orders?status=NotARealStatus");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.Equal(2, page!.Items.Count);
    }

    [Fact]
    public async Task GetOrders_OrdersByNewestFirst()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var companyId = Guid.NewGuid();
        await using (var dbContext = CreateDbContext(factory))
        {
            dbContext.Orders.Add(new Order(Guid.NewGuid(), companyId, "OLD", 100m, OrderStatus.Completed, DateTime.UtcNow.AddDays(-5), null));
            dbContext.Orders.Add(new Order(Guid.NewGuid(), companyId, "NEW", 100m, OrderStatus.Completed, DateTime.UtcNow, null));
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/orders");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.Equal("NEW", page!.Items[0].OrderNumber);
        Assert.Equal("OLD", page.Items[1].OrderNumber);
    }

    private static CompanyManagementDbContext CreateDbContext(TestWebApplicationFactory factory) =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<CompanyManagementDbContext>();
}
