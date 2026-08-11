using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Api.Endpoints;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Platform.PlatformRolePermissions;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Fills the remaining Platform-auth lifecycle gaps not covered by <see cref="PlatformAuthEndpointsDbTests"/>
/// or <see cref="PlatformSchemeIsolationTests"/>: disabled-user rejection, logout/refresh-token
/// revocation round-trip, and the actual resolved role/permission claim set for a fully-privileged
/// Platform role. Needs a Docker daemon — same caveat as PlatformAuthEndpointsDbTests.
/// </summary>
public class PlatformAuthLifecycleDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public PlatformAuthLifecycleDbTests(PostgresApiFactory factory)
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

    private async Task DisablePlatformUserAsync(Guid platformUserId)
    {
        // PlatformUser has no domain mutator that reaches PlatformUserStatus.Disabled today (see the
        // final verification report's Findings section) — this raw SQL flip exists purely to exercise
        // PlatformLoginCommandHandler's existing status check from a fixture, not to imply a supported
        // application code path.
        using IServiceScope scope = _factory.Services.CreateScope();
        MyCondoDbContext db = scope.ServiceProvider.GetRequiredService<MyCondoDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE platform.platform_users SET status = 1 WHERE id = {platformUserId}");
    }

    private async Task<PlatformRoleId> SeedRoleWithAllPlatformPermissionsAsync(string roleName)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformRoleRepository roles = scope.ServiceProvider.GetRequiredService<IPlatformRoleRepository>();
        IPlatformRolePermissionRepository rolePermissions =
            scope.ServiceProvider.GetRequiredService<IPlatformRolePermissionRepository>();
        IPermissionRepository permissions = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        PlatformRole role = PlatformRole.CreateSystem(
            PlatformRoleId.New(), roleName, "Full platform permission set (test fixture).", clock.UtcNow);
        roles.Add(role);

        List<Permission> platformPermissions = await permissions.GetByModuleAsync("platform", CancellationToken.None);
        platformPermissions.Should().NotBeEmpty("the platform permission catalogue must be seeded before this test runs");

        foreach (Permission permission in platformPermissions)
        {
            rolePermissions.Add(new PlatformRolePermission(role.Id, permission.Id, clock.UtcNow, grantedBy: null));
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return role.Id;
    }

    private async Task AssignRoleAsync(Guid platformUserId, PlatformRoleId roleId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformUserRoleAssignmentRepository assignments =
            scope.ServiceProvider.GetRequiredService<IPlatformUserRoleAssignmentRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        assignments.Add(PlatformUserRoleAssignment.Grant(new PlatformUserId(platformUserId), roleId, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Login_With_Disabled_User_Is_Rejected()
    {
        (Guid platformUserId, string password) = await SeedPlatformUserAsync("platform-disabled-check@mycondo.com");
        await DisablePlatformUserAsync(platformUserId);

        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new
        {
            email = "platform-disabled-check@mycondo.com",
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_Grants_Exactly_The_Full_Platform_Permission_Set_For_A_Fully_Privileged_Role()
    {
        (Guid platformUserId, string password) = await SeedPlatformUserAsync("platform-permissions-check@mycondo.com");
        PlatformRoleId roleId = await SeedRoleWithAllPlatformPermissionsAsync("FullPlatformAccess");
        await AssignRoleAsync(platformUserId, roleId);

        List<string> expectedPermissions;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IPermissionRepository permissions = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
            expectedPermissions = (await permissions.GetByModuleAsync("platform", CancellationToken.None))
                .Select(p => p.Name)
                .ToList();
        }

        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new
        {
            email = "platform-permissions-check@mycondo.com",
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PlatformAuthResponse? tokens = await response.Content.ReadFromJsonAsync<PlatformAuthResponse>(JsonOptions);
        tokens.Should().NotBeNull();

        JwtClaims claims = JwtTestHelper.Decode(tokens!.AccessToken);
        claims.GetClaimValues(ClaimTypes.Role).Should().Contain("FullPlatformAccess");
        claims.GetClaimValues("perm").Should().BeEquivalentTo(expectedPermissions);

        tokens.User.Roles.Should().Contain("FullPlatformAccess");
        tokens.User.Permissions.Should().BeEquivalentTo(expectedPermissions);
    }

    [Fact]
    public async Task Logout_Revokes_The_Refresh_Token_So_It_Cannot_Be_Reused()
    {
        (Guid _, string password) = await SeedPlatformUserAsync("platform-logout-check@mycondo.com");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new
        {
            email = "platform-logout-check@mycondo.com",
            password,
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PlatformAuthResponse? tokens = await loginResponse.Content.ReadFromJsonAsync<PlatformAuthResponse>(JsonOptions);

        loginResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieHeaders).Should().BeTrue();
        string rawRefreshToken = ExtractCookieValue(setCookieHeaders!, "mycondo_platform_rt");

        HttpRequestMessage logoutRequest = new(HttpMethod.Post, "/api/v1/platform/auth/logout");
        logoutRequest.Headers.Authorization = new("Bearer", tokens!.AccessToken);
        HttpResponseMessage logoutResponse = await client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // A fresh client (no cookie jar carried over) presenting the pre-logout refresh token by hand
        // proves the token itself was revoked server-side, not merely that the browser's cookie was
        // cleared — the two are different guarantees and the prompt's spec calls for the former.
        using HttpClient replayClient = _factory.CreateClient();
        HttpRequestMessage refreshRequest = new(HttpMethod.Post, "/api/v1/platform/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"mycondo_platform_rt={rawRefreshToken}");
        HttpResponseMessage refreshResponse = await replayClient.SendAsync(refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string ExtractCookieValue(IEnumerable<string> setCookieHeaders, string cookieName)
    {
        string header = setCookieHeaders.Single(h => h.StartsWith($"{cookieName}=", StringComparison.Ordinal));
        string afterName = header[(cookieName.Length + 1)..];
        int separator = afterName.IndexOf(';');
        return separator >= 0 ? afterName[..separator] : afterName;
    }
}
