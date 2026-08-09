using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 2 (mycondo-docs ADR-020) — proves the legacy tenant SuperAdmin role, for tenants that already
/// have one, keeps authorizing exactly as before after Phase 2 ships. Split into its own tiny class
/// (see OrganizationAdminScopeDbTests's doc comment) since it's the one Phase-2 test that doesn't share
/// the others' "auth" rate-limit budget concerns anyway. Needs a Docker daemon; not executed in the
/// environment this was authored in.
/// </summary>
public class LegacySuperAdminCompatibilityDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public LegacySuperAdminCompatibilityDbTests(PostgresApiFactory factory)
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

    private static async Task<HttpResponseMessage> SendAuthedAsync(
        HttpClient client, HttpMethod method, string url, string accessToken, object? body = null)
    {
        using HttpRequestMessage request = new(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Legacy_Tenant_SuperAdmin_Still_Authorizes_Through_Existing_Permission_Claims()
    {
        // Simulates a pre-Phase-2 tenant: SuperAdmin role/grant/assignment constructed directly, the
        // way OrganizationAdminBootstrapper's predecessor (SuperAdminBootstrapper) used to, bypassing
        // the now-current bootstrap path entirely — proving Phase 2 didn't retroactively break already
        // pre-existing legacy SuperAdmin holders.
        Guid tenantId = await SeedActiveTenantAsync("legacy-superadmin-still-works");
        DateTimeOffset nowUtc;
        string plainPassword = "Correct-Horse-Battery-9";

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();
            IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            nowUtc = clock.UtcNow;

            await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);

            User legacyAdmin = User.Register(tenantId, "legacy-admin@example.com", hasher.Hash(plainPassword), "Legacy Admin", null, nowUtc);
            db.Set<User>().Add(legacyAdmin);

            Role legacySuperAdmin = Role.CreateSystem(RoleId.New(), tenantId, "SuperAdmin", "Legacy full-access role.", nowUtc);
            db.Set<Role>().Add(legacySuperAdmin);

            List<Permission> nonPlatformPermissions = await db.Set<Permission>()
                .AsNoTracking().Where(p => p.Module != "platform").ToListAsync();
            foreach (Permission permission in nonPlatformPermissions)
            {
                db.Set<RolePermission>().Add(new RolePermission(tenantId, legacySuperAdmin.Id, permission.Id, nowUtc, grantedBy: null));
            }

            db.Set<RoleAssignment>().Add(RoleAssignment.Grant(tenantId, legacyAdmin.Id, legacySuperAdmin.Id, buildingId: null, nowUtc));

            await db.SaveChangesAsync();
        }

        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { tenantId, email = "legacy-admin@example.com", password = plainPassword });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto loginTokens = (await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions))!;

        JwtClaims claims = JwtTestHelper.Decode(loginTokens.AccessToken);
        claims.GetClaimValues("perm").Should().Contain("role.manage");

        (await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/roles", loginTokens.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
