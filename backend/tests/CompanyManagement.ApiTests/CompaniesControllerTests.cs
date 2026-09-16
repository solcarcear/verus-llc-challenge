using System.Net;
using System.Net.Http.Json;
using CompanyManagement.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CompanyManagement.ApiTests;

public class CompaniesControllerTests
{
    [Fact]
    public async Task PostCompany_ValidAndRelevantRequest_ReturnsCreatedWithLocationAndBody()
    {
        using var factory = new WebApplicationFactory<Program>();
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
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("", "not-a-url"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCompany_IrrelevantCompanyAndWebsite_ReturnsBadRequest()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Microsoft Corporation", "https://apple.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllCompanies_WhenNoneExist_ReturnsEmptyArray()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>();
        Assert.NotNull(companies);
        Assert.Empty(companies!);
    }

    [Fact]
    public async Task GetAllCompanies_ReturnsPreviouslyCreatedCompanies()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest("Acme Corp", "https://acme.com"));

        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>();
        Assert.NotNull(companies);
        Assert.Single(companies!);
        Assert.Equal("Acme Corp", companies![0].Name);
    }

    [Fact]
    public async Task GetCompanyById_ExistingCompany_ReturnsOkWithCompany()
    {
        using var factory = new WebApplicationFactory<Program>();
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
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/companies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
