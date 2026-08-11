using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Users.Queries.GetUsersForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory), proving
/// GetUsersForTenant/DeactivateUser end-to-end. Needs a Docker daemon — same disclosed limitation as
/// every other DB-backed test in this project.
/// </summary>
public class UserEndpointsDbTests : IClassFixture<PostgresApiFactory>
{
    private const string SeedPassword = "Correct-Horse-Battery-9";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public UserEndpointsDbTests(PostgresApiFactory factory)
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

    private static async Task<AuthTokensDto> RegisterAsync(HttpClient client, Guid tenantId, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email,
            password = SeedPassword,
            fullName = "Test User",
            phoneNumber = (string?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokensDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!;
    }

    private static async Task<HttpResponseMessage> SendAuthedAsync(
        HttpClient client, HttpMethod method, string url, string accessToken, object? body = null)
    {
        using HttpRequestMessage request = new(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Get_Users_Returns_All_Tenant_Users()
    {
        Guid tenantId = await SeedActiveTenantAsync("users-list");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        await RegisterAsync(client, tenantId, "member@example.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/users", ownerTokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<UserSummaryDto>? page = await response.Content.ReadFromJsonAsync<PagedResult<UserSummaryDto>>(JsonOptions);
        page.Should().NotBeNull();
        List<UserSummaryDto> users = page!.Items.ToList();
        users.Select(u => u.Email).Should().BeEquivalentTo(["owner@example.com", "member@example.com"]);
        users.Should().OnlyContain(u => u.IsActive);
    }

    [Fact]
    public async Task Disable_User_Marks_Them_Inactive_In_The_User_List()
    {
        Guid tenantId = await SeedActiveTenantAsync("disable-user");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        HttpResponseMessage disableResponse = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{memberUserId}/disable", ownerTokens.AccessToken);
        disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage listResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/users", ownerTokens.AccessToken);
        PagedResult<UserSummaryDto>? page = await listResponse.Content.ReadFromJsonAsync<PagedResult<UserSummaryDto>>(JsonOptions);

        page!.Items.Should().ContainSingle(u => u.UserId == memberUserId && !u.IsActive);
    }

    [Fact]
    public async Task Disable_Already_Disabled_User_Returns_422()
    {
        Guid tenantId = await SeedActiveTenantAsync("disable-user-twice");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/users/{memberUserId}/disable", ownerTokens.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage secondDisable = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{memberUserId}/disable", ownerTokens.AccessToken);
        secondDisable.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static Guid ParseUserIdFromAccessToken(string accessToken)
    {
        string payload = accessToken.Split('.')[1];
        string padded = payload.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        byte[] json = Convert.FromBase64String(padded);
        using JsonDocument document = JsonDocument.Parse(json);
        return Guid.Parse(document.RootElement.GetProperty("sub").GetString()!);
    }
}
