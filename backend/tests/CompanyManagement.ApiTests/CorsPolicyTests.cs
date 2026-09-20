namespace CompanyManagement.ApiTests;

public class CorsPolicyTests
{
    [Fact]
    public async Task PreflightRequest_FromAngularDevOrigin_ToPostCompanies_IsAccepted()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/companies");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowedOrigins));
        Assert.Equal("http://localhost:4200", Assert.Single(allowedOrigins!));
    }
}
