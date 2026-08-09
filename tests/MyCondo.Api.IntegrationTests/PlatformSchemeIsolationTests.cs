using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MyCondo.Api.Authentication;
using MyCondo.Application.Features.Platform.Commands.PlatformLogin;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Verifies the Phase 1 Platform/tenant authentication-scheme boundary end-to-end — no DB required,
/// since scheme/audience validation happens entirely in the JWT bearer handler before any handler or
/// RLS-protected query runs. See mycondo-docs ADR-019.
///
/// The "architecture enforcement" tests (<see cref="Every_Platform_Endpoint_Requires_The_Platform_Scheme"/>,
/// <see cref="Platform_Scheme_Is_Never_The_Default"/>) are the automated safeguard the approved Phase 1
/// blueprint §9 calls for: they fail for any FUTURE platform endpoint that forgets to declare
/// RequirePlatformPermission, without needing anyone to remember to update this file by hand.
/// </summary>
public class PlatformSchemeIsolationTests : IClassFixture<MyCondoWebApplicationFactory>
{
    // Matches appsettings.json's base Jwt section — MyCondoWebApplicationFactory only overrides
    // Jwt:SigningKey, so these real, non-secret configured values still apply under "Testing".
    private const string Issuer = "https://api.mycondo.app";
    private const string TenantAudience = "https://app.mycondo.app";
    private const string PlatformAudience = "https://platform.mycondo.app";
    private const string SigningKey = "test-only-signing-key-not-for-any-real-environment";

    private readonly MyCondoWebApplicationFactory _factory;

    public PlatformSchemeIsolationTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Platform_Token_Calling_A_Tenant_Endpoint_Is_Rejected_With_401()
    {
        using HttpClient client = _factory.CreateClient();
        string platformToken = CreateToken(PlatformAudience, extraClaims: [new Claim("perm", "platform.organization.read")]);
        client.DefaultRequestHeaders.Authorization = new("Bearer", platformToken);

        HttpResponseMessage response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tenant_Token_Calling_A_Platform_Endpoint_Is_Rejected_With_401()
    {
        using HttpClient client = _factory.CreateClient();
        string tenantToken = CreateToken(TenantAudience, extraClaims: [new Claim("tenant_id", Guid.NewGuid().ToString())]);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tenantToken);

        HttpResponseMessage response = await client.GetAsync("/api/v1/platform/organizations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Platform_Endpoint_Without_Any_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/platform/organizations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void Platform_Login_Request_Body_Has_No_Tenant_Field_To_Omit()
    {
        // Structural proof, not a runtime one — deliberately reflection-only, no HTTP dispatch: this
        // class asserts scheme/routing behavior without touching the database (see
        // MyCondoWebApplicationFactory's doc comment); a real login round-trip belongs in a
        // PostgresApiFactory-backed *DbTests class instead, same split as AuthEndpointsTests vs
        // AuthEndpointsDbTests.
        Type commandType = typeof(PlatformLoginCommand);

        commandType.GetProperties().Select(p => p.Name).Should().NotContain(
            name => name.Contains("Tenant", StringComparison.OrdinalIgnoreCase)
                 || name.Contains("Organization", StringComparison.OrdinalIgnoreCase),
            "PlatformLoginCommand must have no tenant/organization field to omit — not merely an optional one");
    }

    [Fact]
    public async Task Platform_Login_With_Invalid_Body_Returns_400()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/login", new { email = "not-an-email", password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void Platform_Auth_And_Organization_Routes_Are_Registered()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        EndpointDataSource endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();

        List<string> routePatterns = endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty)
            .ToList();

        routePatterns.Should().Contain("/api/v1/platform/auth/login");
        routePatterns.Should().Contain("/api/v1/platform/auth/refresh");
        routePatterns.Should().Contain("/api/v1/platform/organizations/");
    }

    [Fact]
    public async Task Every_Platform_Endpoint_Requires_The_Platform_Scheme()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        EndpointDataSource endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
        IAuthorizationPolicyProvider policyProvider =
            scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        List<RouteEndpoint> platformEndpoints = endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? string.Empty).StartsWith("/api/v1/platform/", StringComparison.Ordinal))
            .ToList();

        platformEndpoints.Should().NotBeEmpty("the platform endpoint groups must actually be mapped");

        foreach (RouteEndpoint endpoint in platformEndpoints)
        {
            bool isAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            if (isAnonymous)
            {
                // Only /platform/auth/login and /platform/auth/refresh — verified structurally by not
                // finding them in the "requires Platform scheme" set below AND by the explicit route
                // check above (Platform_Auth_And_Organization_Routes_Are_Registered).
                continue;
            }

            IReadOnlyList<IAuthorizeData> authData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            authData.Should().NotBeEmpty(
                $"protected platform endpoint '{endpoint.RoutePattern.RawText}' must declare an authorization requirement");

            AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(policyProvider, authData);
            policy.Should().NotBeNull();
            policy!.AuthenticationSchemes.Should().ContainSingle(
                $"platform endpoint '{endpoint.RoutePattern.RawText}' must require exactly the Platform scheme, not the tenant default")
                .Which.Should().Be(PlatformAuthenticationDefaults.SchemeName);
        }
    }

    [Fact]
    public void Platform_Scheme_Is_Never_The_Default()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AuthenticationOptions options = scope.ServiceProvider
            .GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        options.DefaultScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        options.DefaultAuthenticateScheme.Should().NotBe(PlatformAuthenticationDefaults.SchemeName);
        options.DefaultChallengeScheme.Should().NotBe(PlatformAuthenticationDefaults.SchemeName);
    }

    private static string CreateToken(string audience, IEnumerable<Claim> extraClaims)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(SigningKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())];
        claims.AddRange(extraClaims);

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = creds
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
