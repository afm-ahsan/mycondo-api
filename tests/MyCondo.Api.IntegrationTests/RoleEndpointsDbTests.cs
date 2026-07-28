using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory), proving
/// the SuperAdmin bootstrap (RegisterUserCommandHandler) and the permission catalogue end-to-end.
/// These need a Docker daemon and were NOT executed in the environment they were authored in — see
/// PostgresApiFactory's doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class RoleEndpointsDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public RoleEndpointsDbTests(PostgresApiFactory factory)
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
            password = "correct-horse-battery-staple",
            fullName = "Test User",
            phoneNumber = (string?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokensDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!;
    }

    [Fact]
    public async Task First_User_Of_Tenant_Is_Bootstrapped_As_SuperAdmin()
    {
        Guid tenantId = await SeedActiveTenantAsync("superadmin-bootstrap");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "first-user@example.com");

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Authorization = new("Bearer", tokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RoleSummaryDto>? roles = await response.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions);
        roles.Should().ContainSingle(r => r.Name == "SuperAdmin" && r.IsSystem);
    }

    [Fact]
    public async Task Second_User_Of_Tenant_Is_Not_Bootstrapped_And_Cannot_View_Roles()
    {
        Guid tenantId = await SeedActiveTenantAsync("superadmin-second-user");
        using HttpClient client = _factory.CreateClient();

        await RegisterAsync(client, tenantId, "first-user@example.com");
        AuthTokensDto secondUserTokens = await RegisterAsync(client, tenantId, "second-user@example.com");

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Authorization = new("Bearer", secondUserTokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Bootstrapped_User_Can_Create_A_New_Role()
    {
        Guid tenantId = await SeedActiveTenantAsync("superadmin-create-role");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "owner@example.com");

        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/roles")
        {
            Content = JsonContent.Create(new { name = "Building Manager", description = "Ops" }),
        };
        request.Headers.Authorization = new("Bearer", tokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Permissions_Returns_Full_Seeded_Catalogue()
    {
        Guid tenantId = await SeedActiveTenantAsync("permission-catalogue");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "owner@example.com");

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/permissions");
        request.Headers.Authorization = new("Bearer", tokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<PermissionDto>? permissions = await response.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions);
        permissions.Should().HaveCount(47);
        permissions.Should().Contain(p => p.Name == "role.manage");
        permissions.Should().Contain(p => p.Name == "permission.view");
    }
}
