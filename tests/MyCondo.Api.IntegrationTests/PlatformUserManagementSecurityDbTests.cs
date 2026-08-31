using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Users.Commands.CreatePlatformUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Platform.PlatformRolePermissions;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using MyCondo.Infrastructure.Seed;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Platform-scope analogue of <see cref="PrivilegedUserManagementSecurityDbTests"/> — end-to-end HTTP
/// round-trip tests for the ADR-036 Platform Super Admin protections (M5 backend security regression
/// milestone): self-protection, the <c>platform.user.manageSuperAdmins</c> composition-permission gate,
/// and the last-active-Super-Admin invariant, all through the real endpoints. Platform roles have no
/// HTTP management surface in this MVP slice (see PlatformUserEndpoints' own doc comment), so
/// non-SuperAdmin test roles are seeded directly via the repositories, the same pattern
/// PlatformBootstrapSeederDbTests already uses for the seeded SuperAdmin. Needs a Docker daemon — see
/// PostgresApiFactory's doc comment.
/// </summary>
public class PlatformUserManagementSecurityDbTests : IClassFixture<PostgresApiFactory>
{
    private const string Password = "Correct-Horse-Battery-9";
    private const string SadminPassword = "SAdmin@1357#";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public PlatformUserManagementSecurityDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Seeds the SuperAdmin PlatformRole (with the full platform.* permission set) and the
    /// sadmin@mycondo.com PlatformUser — mirrors PlatformBootstrapSeederDbTests.RunSeederAsync.</summary>
    private async Task RunBootstrapSeederAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        PlatformBootstrapSeeder seeder = new(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PlatformBootstrapSeeder>.Instance);
        await seeder.SeedAsync(CancellationToken.None);
    }

    private async Task<PlatformRoleId> GetSuperAdminRoleIdAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformRoleRepository roles = scope.ServiceProvider.GetRequiredService<IPlatformRoleRepository>();
        PlatformRole role = (await roles.GetByNameAsync("SuperAdmin", CancellationToken.None))!;
        return role.Id;
    }

    /// <summary>
    /// Platform tables carry no RLS/tenant isolation, so every test method in this class shares one
    /// mutable "SuperAdmin" holder set across the one Postgres instance IClassFixture reuses for the
    /// whole class — a test that seeds an extra active SuperAdmin (to prove the "succeeds once a second
    /// holder exists" case) otherwise silently breaks a *different*, order-dependent test asserting
    /// "exactly one active holder." Deactivates every currently-active SuperAdmin holder except
    /// <paramref name="keepActiveUserId"/> directly via the repository (not the endpoint under test),
    /// so a last-holder assertion can rely on a deterministic count regardless of what earlier test
    /// methods left behind or what order xUnit chose to run them in.
    /// </summary>
    private async Task NormalizeToSingleActiveSuperAdminAsync(Guid keepActiveUserId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformRoleRepository roles = scope.ServiceProvider.GetRequiredService<IPlatformRoleRepository>();
        IPlatformUserRoleAssignmentRepository assignments =
            scope.ServiceProvider.GetRequiredService<IPlatformUserRoleAssignmentRepository>();
        IPlatformUserRepository users = scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        PlatformRole superAdminRole = (await roles.GetByNameAsync("SuperAdmin", CancellationToken.None))!;
        List<PlatformUserRoleAssignment> holderAssignments =
            await assignments.GetForRoleAsync(superAdminRole.Id, CancellationToken.None);

        foreach (PlatformUserRoleAssignment assignment in holderAssignments)
        {
            if (assignment.PlatformUserId.Value == keepActiveUserId)
            {
                continue;
            }

            PlatformUser? holder = await users.GetByIdAsync(assignment.PlatformUserId, CancellationToken.None);
            if (holder is { Status: PlatformUserStatus.Active })
            {
                holder.Deactivate(clock.UtcNow);
            }
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>Seeds a non-SuperAdmin PlatformRole granting exactly the given permission names —
    /// there is no HTTP surface for platform role/permission management to do this through, unlike the
    /// tenant-scope GrantCustomRoleAsync helper in PrivilegedUserManagementSecurityDbTests.</summary>
    private async Task<PlatformRoleId> SeedPlatformRoleAsync(string name, params string[] permissionNames)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformRoleRepository roles = scope.ServiceProvider.GetRequiredService<IPlatformRoleRepository>();
        IPlatformRolePermissionRepository rolePermissions =
            scope.ServiceProvider.GetRequiredService<IPlatformRolePermissionRepository>();
        IPermissionRepository permissions = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        PlatformRole role = PlatformRole.CreateSystem(PlatformRoleId.New(), name, "Test role", clock.UtcNow);
        roles.Add(role);

        List<Permission> catalogue = await permissions.GetAllAsync(CancellationToken.None);
        foreach (string permissionName in permissionNames)
        {
            Permission permission = catalogue.Single(p => p.Name == permissionName);
            rolePermissions.Add(new PlatformRolePermission(role.Id, permission.Id, clock.UtcNow, null));
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return role.Id;
    }

    private async Task<Guid> SeedPlatformUserAsync(string email, PlatformRoleId? roleId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformUserRepository users = scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>();
        IPlatformUserRoleAssignmentRepository assignments =
            scope.ServiceProvider.GetRequiredService<IPlatformUserRoleAssignmentRepository>();
        IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        PlatformUser user = PlatformUser.Create(email, hasher.Hash(Password), "Test Platform User", clock.UtcNow);
        users.Add(user);

        if (roleId is PlatformRoleId id)
        {
            assignments.Add(PlatformUserRoleAssignment.Grant(user.Id, id, clock.UtcNow));
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return user.Id.Value;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password = Password)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/platform/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PlatformAuthTokensDto? tokens = await response.Content.ReadFromJsonAsync<PlatformAuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!.AccessToken;
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
    public async Task The_Sole_SuperAdmin_Cannot_Self_Deactivate()
    {
        await RunBootstrapSeederAsync();
        using HttpClient client = _factory.CreateClient();
        string sadminToken = await LoginAsync(client, "sadmin@mycondo.com", SadminPassword);
        using IServiceScope scope = _factory.Services.CreateScope();
        Guid sadminId = (await scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>()
            .GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None))!.Id.Value;

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/platform/users/{sadminId}/deactivate", sadminToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_Lower_Privileged_Actor_Cannot_Deactivate_A_SuperAdmin()
    {
        await RunBootstrapSeederAsync();
        using HttpClient client = _factory.CreateClient();
        using IServiceScope scope = _factory.Services.CreateScope();
        Guid sadminId = (await scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>()
            .GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None))!.Id.Value;

        PlatformRoleId supportRoleId = await SeedPlatformRoleAsync(
            "SupportDeactivator", "platform.user.deactivate", "platform.user.view");
        await SeedPlatformUserAsync("support@mycondo.com", supportRoleId);
        string supportToken = await LoginAsync(client, "support@mycondo.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/platform/users/{sadminId}/deactivate", supportToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_Platforms_Last_Active_SuperAdmin_Cannot_Be_Deactivated_By_Another_Actor()
    {
        await RunBootstrapSeederAsync();
        using HttpClient client = _factory.CreateClient();
        using IServiceScope scope = _factory.Services.CreateScope();
        Guid sadminId = (await scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>()
            .GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None))!.Id.Value;
        await NormalizeToSingleActiveSuperAdminAsync(sadminId);

        // Grants manageSuperAdmins + deactivate WITHOUT itself holding SuperAdmin — isolates the
        // last-holder invariant from the composition-permission check.
        PlatformRoleId adminManagerRoleId = await SeedPlatformRoleAsync(
            "AdminManager", "platform.user.manageSuperAdmins", "platform.user.deactivate", "platform.user.view");
        await SeedPlatformUserAsync("adminmanager@mycondo.com", adminManagerRoleId);
        string adminManagerToken = await LoginAsync(client, "adminmanager@mycondo.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/platform/users/{sadminId}/deactivate", adminManagerToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deactivating_A_SuperAdmin_Succeeds_Once_A_Second_Holder_Exists()
    {
        // Deliberately does NOT target the shared bootstrap-seeded "sadmin@mycondo.com" singleton this
        // test class reuses everywhere else — platform tables have no RLS/tenant isolation, so
        // disabling that one account here would leak into every other test in this class (whichever
        // runs after it, xUnit gives no ordering guarantee). Seeds two dedicated SuperAdmins instead,
        // scoped to this test alone.
        await RunBootstrapSeederAsync();
        using HttpClient client = _factory.CreateClient();
        PlatformRoleId superAdminRoleId = await GetSuperAdminRoleIdAsync();

        Guid targetAdminId = await SeedPlatformUserAsync("target-admin@mycondo.com", superAdminRoleId);
        Guid secondSuperAdminId = await SeedPlatformUserAsync("second-admin@mycondo.com", superAdminRoleId);
        string secondSuperAdminToken = await LoginAsync(client, "second-admin@mycondo.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/platform/users/{targetAdminId}/deactivate", secondSuperAdminToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondSuperAdminId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Self_Revoke_Of_The_SuperAdmin_Role_Is_Rejected()
    {
        await RunBootstrapSeederAsync();
        using HttpClient client = _factory.CreateClient();
        string sadminToken = await LoginAsync(client, "sadmin@mycondo.com", SadminPassword);
        using IServiceScope scope = _factory.Services.CreateScope();
        Guid sadminId = (await scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>()
            .GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None))!.Id.Value;
        PlatformRoleId superAdminRoleId = await GetSuperAdminRoleIdAsync();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Delete, $"/api/v1/platform/users/{sadminId}/roles/{superAdminRoleId.Value}", sadminToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_Actor_Without_ManageSuperAdmins_Cannot_Create_A_New_SuperAdmin()
    {
        await RunBootstrapSeederAsync();
        using HttpClient client = _factory.CreateClient();

        PlatformRoleId supportRoleId = await SeedPlatformRoleAsync(
            "SupportCreator", "platform.user.create", "platform.user.view");
        await SeedPlatformUserAsync("creator@mycondo.com", supportRoleId);
        string creatorToken = await LoginAsync(client, "creator@mycondo.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/platform/users", creatorToken,
            new CreatePlatformUserCommand("Rogue Admin", "rogue@mycondo.com", "Str0ngPassw0rd!", true, "SuperAdmin"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
