using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Roles.Commands.CreateRole;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof of the ADR-014 claims split: a building-scoped role assignment must produce a
/// `bperm` claim (not a `perm` claim) on the next login. Same disclosed limitation as every other
/// PostgresApiFactory-backed test — needs a Docker daemon, run wherever one is available.
/// </summary>
public class BuildingScopeDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid TargetBuilding = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string ScopedPermission = "complaint.manage";

    private readonly PostgresApiFactory _factory;

    public BuildingScopeDbTests(PostgresApiFactory factory)
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
    public async Task Building_Scoped_Assignment_Produces_Bperm_Claim_Not_Perm_Claim()
    {
        Guid tenantId = await SeedActiveTenantAsync("building-scope");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto superAdminTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto staffTokens = await RegisterAsync(client, tenantId, "staff@example.com");

        using HttpRequestMessage createRoleRequest = new(HttpMethod.Post, "/api/v1/roles")
        {
            Content = JsonContent.Create(new { name = "Building Staff", description = "Scoped ops" }),
        };
        createRoleRequest.Headers.Authorization = new("Bearer", superAdminTokens.AccessToken);
        HttpResponseMessage createRoleResponse = await client.SendAsync(createRoleRequest);
        createRoleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        CreateRoleResult? role = await createRoleResponse.Content.ReadFromJsonAsync<CreateRoleResult>(JsonOptions);
        role.Should().NotBeNull();

        using HttpRequestMessage getPermissionsRequest = new(HttpMethod.Get, "/api/v1/permissions");
        getPermissionsRequest.Headers.Authorization = new("Bearer", superAdminTokens.AccessToken);
        HttpResponseMessage getPermissionsResponse = await client.SendAsync(getPermissionsRequest);
        List<PermissionDto>? catalogue = await getPermissionsResponse.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions);
        PermissionDto scopedPermission = catalogue!.Single(p => p.Name == ScopedPermission);

        using HttpRequestMessage grantRequest = new(HttpMethod.Post, $"/api/v1/roles/{role!.RoleId}/permissions")
        {
            Content = JsonContent.Create(new { permissionId = scopedPermission.Id }),
        };
        grantRequest.Headers.Authorization = new("Bearer", superAdminTokens.AccessToken);
        (await client.SendAsync(grantRequest)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using HttpRequestMessage assignRequest = new(HttpMethod.Post, $"/api/v1/roles/{role.RoleId}/assignments")
        {
            Content = JsonContent.Create(new { userId = staffTokens.User.UserId, buildingId = TargetBuilding }),
        };
        assignRequest.Headers.Authorization = new("Bearer", superAdminTokens.AccessToken);
        (await client.SendAsync(assignRequest)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantId,
            email = "staff@example.com",
            password = "correct-horse-battery-staple",
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? freshTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);

        JsonWebToken jwt = new(freshTokens!.AccessToken);
        List<string> bpermClaims = jwt.Claims.Where(c => c.Type == "bperm").Select(c => c.Value).ToList();
        List<string> permClaims = jwt.Claims.Where(c => c.Type == "perm").Select(c => c.Value).ToList();

        bpermClaims.Should().Contain($"{TargetBuilding}|{ScopedPermission}");
        permClaims.Should().NotContain(ScopedPermission,
            "a building-scoped grant must never appear as a tenant-wide `perm` claim");
    }
}
