using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory). These
/// need a Docker daemon and were NOT executed in the environment they were authored in — see
/// PostgresApiFactory's doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class AuthEndpointsDbTests : IClassFixture<PostgresApiFactory>
{
    // The server serializes JSON as camelCase (see MyCondo.Api.DependencyInjection); the response DTOs
    // are plain PascalCase C# records, so deserialization here needs case-insensitive matching —
    // ReadFromJsonAsync<T>() with no options is case-sensitive by default and would silently produce
    // all-default values otherwise.
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public AuthEndpointsDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> SeedActiveTenantAsync(string slug)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision($"Tenant {slug}", slug, clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return tenant.Id.Value;
    }

    [Fact]
    public async Task Register_Login_GetProfile_Logout_HappyPath()
    {
        Guid tenantId = await SeedActiveTenantAsync("happy-path");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "owner@example.com",
            password = "correct-horse-battery-staple",
            fullName = "Test Owner",
            phoneNumber = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? registerTokens = await registerResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        registerTokens.Should().NotBeNull();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantId,
            email = "owner@example.com",
            password = "correct-horse-battery-staple",
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? loginTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        loginTokens.Should().NotBeNull();

        using HttpRequestMessage meRequest = new(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new("Bearer", loginTokens!.AccessToken);
        HttpResponseMessage meResponse = await client.SendAsync(meRequest);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        UserProfileDto? profile = await meResponse.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
        profile!.Email.Should().Be("owner@example.com");

        using HttpRequestMessage logoutRequest = new(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken = loginTokens.RefreshToken }),
        };
        logoutRequest.Headers.Authorization = new("Bearer", loginTokens.AccessToken);
        HttpResponseMessage logoutResponse = await client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Login_With_Correct_Credentials_Succeeds()
    {
        // Standalone regression test for the bug fixed in this slice: Login is AllowAnonymous, so
        // there's no JWT tenant claim yet — before the TenantContextAccessor fallback, RLS's USING
        // clause always evaluated to NULL for anonymous connections, so this query returned zero rows
        // regardless of whether the credentials were correct. Kept separate from the happy-path test
        // above so this specific regression is identifiable on its own, not buried in a longer flow.
        Guid tenantId = await SeedActiveTenantAsync("login-regression");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "login-check@example.com",
            password = "correct-horse-battery-staple",
            fullName = "Login Check",
            phoneNumber = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantId,
            email = "login-check@example.com",
            password = "correct-horse-battery-staple",
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? tokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_With_Valid_Token_Issues_New_Token_Pair()
    {
        Guid tenantId = await SeedActiveTenantAsync("refresh-round-trip");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "refresh-check@example.com",
            password = "correct-horse-battery-staple",
            fullName = "Refresh Check",
            phoneNumber = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? originalTokens = await registerResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);

        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            tenantId,
            refreshToken = originalTokens!.RefreshToken,
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? freshTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        freshTokens.Should().NotBeNull();
        freshTokens!.AccessToken.Should().NotBe(originalTokens.AccessToken);
        freshTokens.RefreshToken.Should().NotBe(originalTokens.RefreshToken);
    }

    [Fact]
    public async Task Register_With_Unknown_Tenant_Returns_404()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = Guid.NewGuid(),
            email = "nobody@example.com",
            password = "correct-horse-battery-staple",
            fullName = "Nobody",
            phoneNumber = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_With_Suspended_Tenant_Returns_403()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision("Suspended Co", "suspended-co", clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenant.Suspend(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = tenant.Id.Value,
            email = "someone@example.com",
            password = "correct-horse-battery-staple",
            fullName = "Someone",
            phoneNumber = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_Duplicate_Email_Returns_409()
    {
        Guid tenantId = await SeedActiveTenantAsync("duplicate-email");
        using HttpClient client = _factory.CreateClient();

        object payload = new
        {
            tenantId,
            email = "duplicate@example.com",
            password = "correct-horse-battery-staple",
            fullName = "First",
            phoneNumber = (string?)null,
        };

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage second = await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
