using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// ADR-033 Task 13H — proves that <c>platform.subscription.read</c>, <c>platform.subscription.manage</c>,
/// and <c>platform.support.access</c> are enforced as three independent permissions on the Task
/// 13A-13D endpoints, not merged. Uses the same hand-crafted-JWT-against-<see cref="MyCondoWebApplicationFactory"/>
/// technique <see cref="PlatformSchemeIsolationTests"/> already established — no database required,
/// since <c>PlatformPermissionEndpointFilter</c> (see MyCondo.Api/Authorization) short-circuits to 403
/// before any handler/DbContext access. Deliberately does NOT use <see cref="PostgresApiFactory"/>: that
/// combination (hand-crafted Platform JWT + real HTTP call) is a known-broken fixture (see
/// OrganizationManagementDbTests), so these tests stay in the not-broken, no-DB lane and only assert the
/// permission-denied side (403), which never reaches a handler.
/// </summary>
public class PlatformSubscriptionAuthorizationSeparationTests : IClassFixture<MyCondoWebApplicationFactory>
{
    // Matches the actual checked-in appsettings.json Jwt section (ADR-033 Task 13H.1 restored
    // Jwt:PlatformAudience as genuinely distinct from the tenant Jwt:Audience). Using the real
    // configured Platform audience here — not an invented one — is required for these tokens to
    // authenticate under the Platform scheme at all, so the 403s these tests assert are proven by the
    // permission filter, not by an audience mismatch.
    private const string Issuer = "https://api.condobd.com";
    private const string PlatformAudience = "https://platform.condobd.com";
    private const string SigningKey = "test-only-signing-key-not-for-any-real-environment";

    private readonly MyCondoWebApplicationFactory _factory;

    public PlatformSubscriptionAuthorizationSeparationTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static string CreateToken(params string[] permissions)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(SigningKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())];
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = Issuer,
            Audience = PlatformAudience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = creds
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private HttpClient CreateAuthorizedClient(params string[] permissions)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(permissions));
        return client;
    }

    [Fact]
    public async Task Subscription_Read_Permission_Alone_Cannot_Change_The_Subscription()
    {
        using HttpClient client = CreateAuthorizedClient("platform.subscription.read");
        Guid organizationId = Guid.NewGuid();

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscription",
            new { SubscriptionPackageVersionId = Guid.NewGuid(), BillingCycle = 0, AutoRenew = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("mark-past-due")]
    [InlineData("reactivate")]
    [InlineData("restrict")]
    [InlineData("cancel")]
    [InlineData("expire")]
    public async Task Subscription_Read_Permission_Alone_Cannot_Perform_Lifecycle_Transitions(string action)
    {
        using HttpClient client = CreateAuthorizedClient("platform.subscription.read");
        Guid organizationId = Guid.NewGuid();

        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscription/{action}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Subscription_Manage_Permission_Alone_Cannot_Read_Or_Write_Feature_Overrides()
    {
        using HttpClient client = CreateAuthorizedClient("platform.subscription.manage");
        Guid organizationId = Guid.NewGuid();

        HttpResponseMessage getResponse = await client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/feature-overrides");
        HttpResponseMessage postResponse = await client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/feature-overrides",
            new { FeatureKey = "facilities.swimming_pool", Enabled = true, EffectiveFrom = DateTimeOffset.UtcNow, EffectiveUntil = (DateTimeOffset?)null, Reason = (string?)null });

        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Support_Access_Permission_Alone_Cannot_Change_The_Subscription_Or_Its_Lifecycle()
    {
        using HttpClient client = CreateAuthorizedClient("platform.support.access");
        Guid organizationId = Guid.NewGuid();

        HttpResponseMessage changeResponse = await client.PatchAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscription",
            new { SubscriptionPackageVersionId = Guid.NewGuid(), BillingCycle = 0, AutoRenew = false });
        HttpResponseMessage restrictResponse = await client.PostAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscription/restrict", content: null);
        HttpResponseMessage readResponse = await client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscription");

        changeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        restrictResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        readResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task No_Permissions_Cannot_Read_The_Subscription_Or_Feature_Overrides()
    {
        using HttpClient client = CreateAuthorizedClient();
        Guid organizationId = Guid.NewGuid();

        HttpResponseMessage subscriptionResponse = await client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscription");
        HttpResponseMessage overridesResponse = await client.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/feature-overrides");

        subscriptionResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        overridesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
