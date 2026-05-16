using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RoboOps.Api.Tests;

public class SessionWorkflowIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private const string RobotId = "11111111-1111-1111-1111-111111111111";
    private const string TaskId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    private readonly HttpClient _client;

    public SessionWorkflowIntegrationTests(TestApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Admin_can_start_session_submit_feedback_and_read_quality()
    {
        var token = await LoginAsync(_client, "admin");

        using var start = new HttpRequestMessage(HttpMethod.Post, "/api/sessions");
        start.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        start.Content = SessionStartContent("integration-operator");

        var created = await _client.SendAsync(start);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var session = await created.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();

        using var feedback = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{sessionId}/feedback");
        feedback.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        feedback.Content = JsonContent.Create(
            new
            {
                reviewer = "integration-reviewer",
                videoQuality = 4,
                controlResponsiveness = 5,
                labelCompleteness = 4,
                notes = "Stable stream and clear labels for this portfolio run."
            });

        var feedbackResponse = await _client.SendAsync(feedback);
        Assert.Equal(HttpStatusCode.Created, feedbackResponse.StatusCode);

        using var qualityRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/sessions/{sessionId}/quality");
        qualityRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var qualityResponse = await _client.SendAsync(qualityRequest);
        qualityResponse.EnsureSuccessStatusCode();

        var quality = await qualityResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(quality.TryGetProperty("score", out var score));
        Assert.True(score.GetDouble() > 0);
    }

    [Fact]
    public async Task Operator_can_start_session_but_cannot_submit_feedback()
    {
        var token = await LoginAsync(_client, "operator");

        using var start = new HttpRequestMessage(HttpMethod.Post, "/api/sessions");
        start.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        start.Content = SessionStartContent("operator-only");

        var created = await _client.SendAsync(start);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var session = await created.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();

        using var feedback = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{sessionId}/feedback");
        feedback.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        feedback.Content = JsonContent.Create(
            new
            {
                reviewer = "operator-attempt",
                videoQuality = 3,
                controlResponsiveness = 3,
                labelCompleteness = 3,
                notes = "Should be forbidden."
            });

        var feedbackResponse = await _client.SendAsync(feedback);
        Assert.Equal(HttpStatusCode.Forbidden, feedbackResponse.StatusCode);
    }

    private static StringContent SessionStartContent(string operatorName)
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["robotId"] = RobotId,
            ["taskId"] = TaskId,
            ["operator"] = operatorName
        });

        return new StringContent(payload, Encoding.UTF8, "application/json");
    }

    private static async Task<string> LoginAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "roboops" });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>();
        return auth.GetProperty("accessToken").GetString()!;
    }
}
