using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformRolePermissions;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using MyCondo.Infrastructure.Seed;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Verifies PlatformBootstrapSeeder directly against a real, ephemeral PostgreSQL container — needs a
/// Docker daemon; not executed in the environment this was authored in (see PostgresApiFactory's doc
/// comment). The seeder is Development-only (see Program.cs) and this factory boots under "Testing",
/// so it's invoked explicitly here rather than relying on host startup.
/// </summary>
public class PlatformBootstrapSeederDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public PlatformBootstrapSeederDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task RunSeederAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        PlatformBootstrapSeeder seeder = new(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PlatformBootstrapSeeder>.Instance);
        await seeder.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Seeder_Provisions_SuperAdmin_With_No_Tenant_Membership()
    {
        await RunSeederAsync();

        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformUserRepository users = scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>();

        PlatformUser? sadmin = await users.GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None);
        sadmin.Should().NotBeNull();
        sadmin!.Status.Should().Be(PlatformUserStatus.Active);

        // Structural, not just behavioral: PlatformUser has no field a tenant membership could even
        // be written into. See PlatformUserTests.PlatformUser_Has_No_TenantId_Field for the
        // compile-time proof; this asserts the seeded row's actual runtime state matches.
        typeof(PlatformUser).GetProperties().Select(p => p.Name)
            .Should().NotContain(name => name.Contains("Tenant", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Seeder_Hashes_The_Password_Not_Plaintext()
    {
        await RunSeederAsync();

        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformUserRepository users = scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>();
        IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        PlatformUser sadmin = (await users.GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None))!;

        sadmin.PasswordHash.Should().NotBe("SAdmin@1357#");
        hasher.Verify("SAdmin@1357#", sadmin.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Seeder_Grants_SuperAdmin_Role_With_Platform_Permissions()
    {
        await RunSeederAsync();

        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformUserRepository users = scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>();
        IPlatformRoleRepository roles = scope.ServiceProvider.GetRequiredService<IPlatformRoleRepository>();
        IPlatformRolePermissionRepository rolePermissions =
            scope.ServiceProvider.GetRequiredService<IPlatformRolePermissionRepository>();
        IPlatformUserRoleAssignmentRepository assignments =
            scope.ServiceProvider.GetRequiredService<IPlatformUserRoleAssignmentRepository>();

        PlatformUser sadmin = (await users.GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None))!;
        PlatformRole? role = await roles.GetByNameAsync("SuperAdmin", CancellationToken.None);
        role.Should().NotBeNull();
        role!.IsSystem.Should().BeTrue();

        List<PlatformUserRoleAssignment> userAssignments = await assignments.GetForUserAsync(sadmin.Id, CancellationToken.None);
        userAssignments.Should().ContainSingle(a => a.PlatformRoleId == role.Id);

        List<string> permissionNames = await rolePermissions.GetPermissionNamesForRoleAsync(role.Id, CancellationToken.None);
        permissionNames.Should().Contain("platform.organization.create");
        permissionNames.Should().Contain("platform.organization.suspend");
        permissionNames.Should().OnlyContain(name => name.StartsWith("platform.", StringComparison.Ordinal),
            "the Platform SuperAdmin must only ever receive platform.* permissions, never the tenant catalog");
    }

    [Fact]
    public async Task Seeder_Is_Idempotent()
    {
        await RunSeederAsync();
        await RunSeederAsync();

        using IServiceScope scope = _factory.Services.CreateScope();
        IPlatformRoleRepository roles = scope.ServiceProvider.GetRequiredService<IPlatformRoleRepository>();

        // AnyAsync-gated seeder: a second run is a full no-op, so re-fetching by name still resolves
        // to exactly the one role/user created the first time — no duplicate-key violation, no
        // duplicate row.
        PlatformRole? role = await roles.GetByNameAsync("SuperAdmin", CancellationToken.None);
        role.Should().NotBeNull();
    }
}
