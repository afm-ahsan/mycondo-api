using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace MyCondo.Api.IntegrationTests;

// Covers the dedicated "auth" rate-limit policy added in UX-6 production hardening (10 requests/
// minute per client, applied to Login and Register — see DependencyInjection.AddApiServices). These
// requests never reach a handler that needs a database (invalid/nonexistent tenant short-circuits
// first), so — like AuthEndpointsTests — this runs against MyCondoWebApplicationFactory, not a real
// Postgres.
//
// Each scenario gets its OWN class (and therefore its own IClassFixture<MyCondoWebApplicationFactory>
// instance, since xUnit creates one fixture instance per test class, shared across all [Fact]s in
// that class) — the "auth" policy partitions only by client IP, not by IP+endpoint, so Login and
// Register share one bucket; running them in the same class against the same factory would let one
// test's requests silently consume the other's quota.

public class LoginRateLimitingTests : IClassFixture<MyCondoWebApplicationFactory>
{
    private readonly MyCondoWebApplicationFactory _factory;

    public LoginRateLimitingTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Is_Rate_Limited_After_10_Requests_Per_Minute()
    {
        using HttpClient client = _factory.CreateClient();
        object body = new
        {
            tenantId = "00000000-0000-0000-0000-000000000000",
            email = "someone@example.com",
            password = "whatever-password",
        };

        List<HttpStatusCode> statusCodes = [];
        for (int i = 0; i < 11; i++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", body);
            statusCodes.Add(response.StatusCode);
        }

        // The first 10 requests are governed by normal request handling (each one 400s here, since
        // the tenant doesn't exist — that's fine, the rate limiter runs before the handler regardless
        // of what the handler would have returned). The 11th must be rejected by the limiter itself.
        statusCodes.Take(10).Should().NotContain(HttpStatusCode.TooManyRequests);
        statusCodes[10].Should().Be(HttpStatusCode.TooManyRequests);
    }
}

public class RegisterRateLimitingTests : IClassFixture<MyCondoWebApplicationFactory>
{
    private readonly MyCondoWebApplicationFactory _factory;

    public RegisterRateLimitingTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Is_Rate_Limited_After_10_Requests_Per_Minute()
    {
        using HttpClient client = _factory.CreateClient();

        List<HttpStatusCode> statusCodes = [];
        for (int i = 0; i < 11; i++)
        {
            object body = new
            {
                tenantId = "00000000-0000-0000-0000-000000000000",
                email = $"someone{i}@example.com",
                password = "Correct-Horse-Battery-9",
                fullName = "Someone",
                phoneNumber = (string?)null,
            };
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", body);
            statusCodes.Add(response.StatusCode);
        }

        statusCodes.Take(10).Should().NotContain(HttpStatusCode.TooManyRequests);
        statusCodes[10].Should().Be(HttpStatusCode.TooManyRequests);
    }
}

public class RefreshRateLimitingTests : IClassFixture<MyCondoWebApplicationFactory>
{
    private readonly MyCondoWebApplicationFactory _factory;

    public RefreshRateLimitingTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Refresh_Is_Not_Subject_To_The_Auth_Rate_Limit()
    {
        // /refresh uses a possession-based HttpOnly cookie, not a guessable credential — it
        // deliberately does not carry the "auth" policy (see AuthEndpoints.cs), only the generous
        // global limiter. 11 rapid requests should never see a 429 from the "auth" policy here.
        using HttpClient client = _factory.CreateClient();
        object body = new { tenantId = "00000000-0000-0000-0000-000000000000" };

        List<HttpStatusCode> statusCodes = [];
        for (int i = 0; i < 11; i++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/refresh", body);
            statusCodes.Add(response.StatusCode);
        }

        statusCodes.Should().NotContain(HttpStatusCode.TooManyRequests);
    }
}
