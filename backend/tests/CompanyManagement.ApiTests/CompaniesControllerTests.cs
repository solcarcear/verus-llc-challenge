using System.Net;
using System.Net.Http.Json;
using CompanyManagement.Api.Contracts;

namespace CompanyManagement.ApiTests;

public class CompaniesControllerTests
{
    [Fact]
    public async Task PostCompany_ValidAndRelevantRequest_ReturnsCreatedWithLocationAndBody()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal("Acme Corp", body.Name);
        Assert.Equal("https://acme.com", body.WebsiteUrl);
        Assert.Contains(body.Id.ToString(), response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task PostCompany_InvalidRequest_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("", "not-a-url"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCompany_IrrelevantCompanyAndWebsite_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Microsoft Corporation", "https://apple.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllCompanies_WhenNoneExist_ReturnsEmptyPage()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();
        Assert.NotNull(page);
        Assert.Empty(page!.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task GetAllCompanies_ReturnsPreviouslyCreatedCompanies()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));

        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();
        Assert.NotNull(page);
        Assert.Single(page!.Items);
        Assert.Equal("Acme Corp", page.Items[0].Name);
    }

    [Fact]
    public async Task GetAllCompanies_UsesDefaultPageNumberAndPageSize_WhenNotSpecified()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/companies");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();
        Assert.Equal(1, page!.PageNumber);
        Assert.Equal(20, page.PageSize);
    }

    [Fact]
    public async Task GetAllCompanies_FirstPage_ReturnsExpectedNumberOfRecords()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await CreateCompaniesAsync(client, count: 25);

        var response = await client.GetAsync("/api/companies?pageNumber=1&pageSize=10");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();
        Assert.Equal(10, page!.Items.Count);
        Assert.Equal(25, page.TotalCount);
    }

    [Fact]
    public async Task GetAllCompanies_SecondPage_ReturnsDifferentRecordsThanFirstPage()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await CreateCompaniesAsync(client, count: 25);

        var firstPageResponse = await client.GetAsync("/api/companies?pageNumber=1&pageSize=10");
        var secondPageResponse = await client.GetAsync("/api/companies?pageNumber=2&pageSize=10");

        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();

        Assert.Equal(10, secondPage!.Items.Count);
        var firstPageIds = firstPage!.Items.Select(c => c.Id).ToHashSet();
        Assert.DoesNotContain(secondPage.Items, c => firstPageIds.Contains(c.Id));
    }

    [Fact]
    public async Task GetAllCompanies_InvalidPageNumber_FallsBackToFirstPage()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await CreateCompaniesAsync(client, count: 3);

        var response = await client.GetAsync("/api/companies?pageNumber=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();
        Assert.Equal(1, page!.PageNumber);
        Assert.Equal(3, page.Items.Count);
    }

    [Fact]
    public async Task GetAllCompanies_PageSizeAboveMaximum_IsCappedAtMaximum()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/companies?pageSize=500");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();
        Assert.Equal(100, page!.PageSize);
    }

    [Fact]
    public async Task GetAllCompanies_TotalPages_IsCalculatedFromTotalCountAndPageSize()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await CreateCompaniesAsync(client, count: 25);

        var response = await client.GetAsync("/api/companies?pageSize=10");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CompanyResponse>>();
        Assert.Equal(25, page!.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    private static async Task CreateCompaniesAsync(HttpClient client, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await client.PostAsJsonAsync(
                "/api/companies",
                new CreateCompanyRequest($"Acme{i} Corp", $"https://acme{i}.com"));
        }
    }

    [Fact]
    public async Task GetCompanyById_ExistingCompany_ReturnsOkWithCompany()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        var response = await client.GetAsync($"/api/companies/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var company = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(company);
        Assert.Equal(created.Id, company!.Id);
        Assert.Equal(created.Name, company.Name);
    }

    [Fact]
    public async Task GetCompanyById_UnknownId_ReturnsNotFound()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/companies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCompanies_WithSearchQuery_ReturnsMatchingCompanies()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Microsoft Corporation", "https://microsoft.com"));
        await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Acme Corp", "https://acme.com"));

        var response = await client.GetAsync("/api/companies?search=micro");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>();
        Assert.NotNull(companies);
        Assert.Single(companies!);
        Assert.Equal("Microsoft Corporation", companies![0].Name);
    }

    [Fact]
    public async Task GetCompanies_WithDomainSearchQuery_ReturnsMatchingCompany()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Acme Corp", "https://acme.com"));
        await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Microsoft Corporation", "https://microsoft.com"));

        var response = await client.GetAsync("/api/companies?search=acme.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>();
        Assert.NotNull(companies);
        Assert.Single(companies!);
        Assert.Equal("Acme Corp", companies![0].Name);
    }

    [Fact]
    public async Task GetCompanies_WithSearchQuery_ReturnsResultsInRelevanceOrder()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Global Acme Partners", "https://globalacme.com"));
        await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Acme", "https://acme.com"));

        var response = await client.GetAsync("/api/companies?search=acme");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>();
        Assert.NotNull(companies);
        Assert.Equal(2, companies!.Count);
        Assert.Equal("Acme", companies[0].Name);
        Assert.Equal("Global Acme Partners", companies[1].Name);
    }

    [Fact]
    public async Task GetCompanies_WithSearchQuery_ReturnsOkWithEmptyArray_WhenNothingMatches()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Acme Corp", "https://acme.com"));

        var response = await client.GetAsync("/api/companies?search=nonexistent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>();
        Assert.NotNull(companies);
        Assert.Empty(companies!);
    }

    [Fact]
    public async Task PutCompany_ExistingCompany_ReturnsOkWithUpdatedBody()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/companies/{created!.Id}",
            new UpdateCompanyRequest("Acme Global", "https://acmeglobal.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Acme Global", updated.Name);
        Assert.Equal("https://acmeglobal.com", updated.WebsiteUrl);
    }

    [Fact]
    public async Task PutCompany_PersistsTheChange_VisibleOnSubsequentGet()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        await client.PutAsJsonAsync(
            $"/api/companies/{created!.Id}",
            new UpdateCompanyRequest("Acme Global", "https://acmeglobal.com"));
        var getResponse = await client.GetAsync($"/api/companies/{created.Id}");

        var fetched = await getResponse.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.Equal("Acme Global", fetched!.Name);
    }

    [Fact]
    public async Task PutCompany_UnknownId_ReturnsNotFound()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/companies/{Guid.NewGuid()}",
            new UpdateCompanyRequest("Acme Global", "https://acmeglobal.com"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutCompany_InvalidRequest_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/companies/{created!.Id}",
            new UpdateCompanyRequest("", "not-a-url"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCompany_IrrelevantNameAndWebsite_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/companies/{created!.Id}",
            new UpdateCompanyRequest("Microsoft Corporation", "https://apple.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCompany_ExistingCompany_ReturnsNoContent()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        var response = await client.DeleteAsync($"/api/companies/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCompany_RemovesItFromSubsequentGetAll()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        await client.DeleteAsync($"/api/companies/{created!.Id}");
        var getResponse = await client.GetAsync($"/api/companies/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCompany_UnknownId_ReturnsNotFound()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/companies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
