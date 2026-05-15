using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoboOps.Api.Tests;

public class ApiIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(TestApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok_and_service_name()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("healthy", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("RoboOps.Api", doc.RootElement.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Login_rejects_invalid_credentials()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_token_for_demo_admin()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "roboops" });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.Equal("Admin", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Robots_requires_authorization()
    {
        var response = await _client.GetAsync("/api/robots");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Robots_returns_seed_data_when_authenticated()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "roboops" });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = auth.GetProperty("accessToken").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/robots");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var robots = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, robots!.ValueKind);
        Assert.True(robots.GetArrayLength() >= 2);
    }
}
