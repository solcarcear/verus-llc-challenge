using System.Net;
using System.Net.Http.Json;
using CompanyManagement.Api.Contracts;
using CompanyManagement.Domain;
using CompanyManagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyManagement.ApiTests;

public class ContactsControllerTests
{
    [Fact]
    public async Task GetContacts_WhenNoneExist_ReturnsEmptyPage()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/contacts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ContactResponse>>();
        Assert.Empty(page!.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task GetContacts_FiltersByCompanyId()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var (companyAId, companyBId) = await SeedTwoCompaniesWithContactsAsync(factory);

        var response = await client.GetAsync($"/api/contacts?companyId={companyAId}");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ContactResponse>>();
        Assert.Equal(2, page!.Items.Count);
        Assert.All(page.Items, c => Assert.Equal(companyAId, c.CompanyId));
        _ = companyBId;
    }

    [Fact]
    public async Task GetContacts_FiltersByIsActive()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var companyId = Guid.NewGuid();
        await using var dbContext = CreateDbContext(factory);
        dbContext.Contacts.Add(new Contact(Guid.NewGuid(), companyId, "Active", "One", "active1@test.example", null, null, true, DateTime.UtcNow));
        dbContext.Contacts.Add(new Contact(Guid.NewGuid(), companyId, "Inactive", "One", "inactive1@test.example", null, null, false, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync("/api/contacts?isActive=true");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ContactResponse>>();
        var item = Assert.Single(page!.Items);
        Assert.True(item.IsActive);
    }

    [Fact]
    public async Task GetContacts_RespectsPageSize()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var companyId = Guid.NewGuid();
        await using (var dbContext = CreateDbContext(factory))
        {
            for (var i = 0; i < 5; i++)
            {
                dbContext.Contacts.Add(new Contact(
                    Guid.NewGuid(), companyId, "First", $"Last{i}", $"contact{i}@test.example", null, null, true, DateTime.UtcNow));
            }

            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/contacts?pageNumber=1&pageSize=2");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ContactResponse>>();
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    private static CompanyManagementDbContext CreateDbContext(TestWebApplicationFactory factory) =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<CompanyManagementDbContext>();

    private static async Task<(Guid CompanyAId, Guid CompanyBId)> SeedTwoCompaniesWithContactsAsync(TestWebApplicationFactory factory)
    {
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();
        await CompaniesControllerTests.SeedContactsAndOrdersAsync(factory, companyAId, contactCount: 2, orderCount: 0);
        await CompaniesControllerTests.SeedContactsAndOrdersAsync(factory, companyBId, contactCount: 1, orderCount: 0);

        return (companyAId, companyBId);
    }
}
