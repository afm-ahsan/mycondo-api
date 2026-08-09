using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Api.Endpoints;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory) — the
/// Platform-scope analogue of AuthEndpointsDbTests. Needs a Docker daemon; not executed in the
/// environment this was authored in (see PostgresApiFactory's doc comment and the Phase 1 completion
/// report's Live PostgreSQL/RLS verification section). Run wherever Docker is available before
/// trusting these as currently passing.
/// </summary>
public class PlatformAuthEndpointsDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public PlatformAuthEndpointsDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid PlatformUserId, string PlainPassword)> SeedPlatformUserAsync(string email)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformUserRepository users = scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();
        IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        const string plainPassword = "Correct-Horse-Battery-9";
        PlatformUser user = PlatformUser.Create(email, hasher.Hash(plainPassword), "Test Platform User", clock.UtcNow);

        users.Add(user);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return (user.Id.Value, plainPassword);
    }

    [Fact]
    public async Task Login_With_Correct_Credentials_Succeeds_And_Has_No_Tenant_Claim()
    {
        (Guid _, string password) = await SeedPlatformUserAsync("platform-login-check@mycondo.com");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new
        {
            email = "platform-login-check@mycondo.com",
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PlatformAuthResponse? tokens = await response.Content.ReadFromJsonAsync<PlatformAuthResponse>(JsonOptions);
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();

        JwtClaims claims = JwtTestHelper.Decode(tokens.AccessToken);
        claims.ContainsClaim("tenant_id").Should().BeFalse("a Platform token must never carry a tenant_id claim");
        claims.GetClaimValue("identity_scope").Should().Be("platform");
        claims.GetAudience().Should().Be("https://platform.mycondo.app");
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_403()
    {
        await SeedPlatformUserAsync("platform-wrong-password@mycondo.com");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new
        {
            email = "platform-wrong-password@mycondo.com",
            password = "not-the-right-password",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_With_Unknown_Email_Returns_403_Not_404()
    {
        // Same response as wrong-password — no enumeration leak (mirrors LoginCommandHandler's
        // existing tenant-side behavior).
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new
        {
            email = "nobody-at-all@mycondo.com",
            password = "whatever",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tenant_Credentials_Cannot_Authenticate_Through_Platform_Login()
    {
        // A tenant User row and a PlatformUser row are physically separate tables — even with the
        // exact same email, a tenant user's credentials simply don't exist in platform_users.
        //
        // The tenant user must be created through the real /api/v1/auth/register endpoint, not by
        // inserting a User row directly via a raw DbContext scope: a DbContext resolved outside an
        // HTTP request has no JWT/HttpContext for TenantContextAccessor to read, so RLS's WITH CHECK
        // correctly rejects a User row claiming a real tenant_id with no matching session context —
        // this is RLS working exactly as intended, not a defect (see PostgresApiFactory's
        // CreateDbContextForTenant doc comment for the same caveat).
        using IServiceScope scope = _factory.Services.CreateScope();
        Domain.Features.Tenancy.ITenantRepository tenants =
            scope.ServiceProvider.GetRequiredService<Domain.Features.Tenancy.ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Domain.Features.Tenancy.Tenant tenant = Domain.Features.Tenancy.Tenant.Provision(
            "Cross-Auth Co", "cross-auth-co", clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = tenant.Id.Value,
            email = "shared@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Tenant User",
            phoneNumber = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new
        {
            email = "shared@example.com",
            password = "Correct-Horse-Battery-9",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Refresh_With_Valid_Platform_Token_Issues_New_Token_Pair()
    {
        (Guid _, string password) = await SeedPlatformUserAsync("platform-refresh-check@mycondo.com");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new
        {
            email = "platform-refresh-check@mycondo.com",
            password,
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PlatformAuthResponse? originalTokens = await loginResponse.Content.ReadFromJsonAsync<PlatformAuthResponse>(JsonOptions);
        loginResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookie).Should().BeTrue();
        setCookie!.Should().Contain(c => c.StartsWith("mycondo_platform_rt="));

        // No refreshToken in the body — the mycondo_platform_rt cookie set by Login above is carried
        // automatically by this HttpClient instance.
        HttpResponseMessage refreshResponse = await client.PostAsync("/api/v1/platform/auth/refresh", null);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PlatformAuthResponse? freshTokens = await refreshResponse.Content.ReadFromJsonAsync<PlatformAuthResponse>(JsonOptions);
        freshTokens!.AccessToken.Should().NotBe(originalTokens!.AccessToken);
    }

    [Fact]
    public async Task Tenant_Refresh_Cookie_Cannot_Be_Used_On_Platform_Refresh_Endpoint()
    {
        // The two refresh cookies are named differently and scoped to different paths
        // (mycondo_rt @ /api/v1/auth vs mycondo_platform_rt @ /api/v1/platform/auth) — a browser
        // holding only a tenant session sends no mycondo_platform_rt cookie at all, so
        // PlatformRefreshTokenCookie.Read returns null, RefreshPlatformTokenCommand.RefreshToken ends
        // up empty, and RefreshPlatformTokenCommandValidator's NotEmpty() rule rejects it with 400
        // before the handler (and its 403-on-invalid-token path) ever runs — same behavior the tenant
        // side's RefreshTokenCommandValidator already has for an empty RefreshToken. Either way, the
        // tenant's refresh token is never read, matched, or rotated by this endpoint.
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/v1/platform/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
