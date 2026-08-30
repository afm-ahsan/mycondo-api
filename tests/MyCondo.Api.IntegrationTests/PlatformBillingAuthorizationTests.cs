using System.Net;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Proves the Task 14D Platform billing read-model endpoints (<c>/api/v1/platform/billing/...</c>) are
/// gated behind Platform authentication and the <c>platform.subscription.read</c> permission — same
/// hand-crafted-JWT-against-<see cref="MyCondoWebApplicationFactory"/> technique
/// <see cref="PlatformSubscriptionAuthorizationSeparationTests"/> already established. No database
/// required: <c>PlatformPermissionEndpointFilter</c> short-circuits to 403 before any handler/DbContext
/// access.
/// </summary>
public class PlatformBillingAuthorizationTests : IClassFixture<MyCondoWebApplicationFactory>
{
    private const string Issuer = "https://api.condobd.com";
    private const string PlatformAudience = "https://platform.condobd.com";
    private const string SigningKey = "test-only-signing-key-not-for-any-real-environment";

    private readonly MyCondoWebApplicationFactory _factory;

    public PlatformBillingAuthorizationTests(MyCondoWebApplicationFactory factory)
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

    [Theory]
    [InlineData("/api/v1/platform/billing/invoices")]
    [InlineData("/api/v1/platform/billing/outstanding")]
    public async Task No_Permissions_Cannot_Read_The_Billing_Read_Model(string path)
    {
        using HttpClient client = CreateAuthorizedClient();

        HttpResponseMessage response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task No_Permissions_Cannot_Read_An_Invoice_Detail()
    {
        using HttpClient client = CreateAuthorizedClient();
        Guid invoiceId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/platform/billing/invoices/{invoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task No_Permissions_Cannot_Read_Invoice_Payment_History()
    {
        using HttpClient client = CreateAuthorizedClient();
        Guid invoiceId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/platform/billing/invoices/{invoiceId}/payments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_Unrelated_Platform_Permission_Alone_Cannot_Read_The_Billing_Read_Model()
    {
        using HttpClient client = CreateAuthorizedClient("platform.organization.read");

        HttpResponseMessage response = await client.GetAsync("/api/v1/platform/billing/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unauthenticated_Requests_Are_Rejected()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/platform/billing/invoices");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
