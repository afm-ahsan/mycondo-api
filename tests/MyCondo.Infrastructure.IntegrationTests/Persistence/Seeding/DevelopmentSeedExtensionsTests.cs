using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Identity;
using MyCondo.Infrastructure.Persistence.Seeding.Extensions;

namespace MyCondo.Infrastructure.IntegrationTests.Persistence.Seeding;

/// <summary>
/// End-to-end (in-memory) proof of standard #12 from mycondo-seed-data-architecture-refactor-v2.md:
/// fresh-database seeding succeeds, and running it again produces no duplicates. Uses the real
/// SuperAdminBootstrapper/DefaultRoleCatalogueSeeder/Argon2idPasswordHasher services against fake
/// in-memory repositories rather than mocks, so this exercises the actual production seeding logic —
/// only the storage layer is faked, standing in for a real PostgreSQL instance
/// (Testcontainers-backed RLS enforcement is covered separately by MyCondo.MultiTenancyTests).
/// </summary>
public class DevelopmentSeedExtensionsTests
{
    // Mirrors DefaultRoleCatalogueSeederTests' catalogue — every permission name referenced by the
    // seven default roles, so DefaultRoleCatalogueSeeder.SeedAsync doesn't throw on an unknown name.
    private static readonly string[] FullCatalogueNames =
    [
        "audit.view", "billing.generate", "billing.rule.manage", "billing.rule.view",
        "complaint.assign", "complaint.create", "complaint.manage", "complaint.view",
        "document.delete", "document.upload", "document.view", "expense.manage", "expense.view",
        "invoice.view", "invoice.void", "lease.manage", "lease.view", "notification.manage",
        "notification.view", "ownership.manage", "ownership.view", "payment.record",
        "payment.reverse", "payment.view", "permission.view", "property.create", "property.delete",
        "property.update", "property.view", "report.financial.view", "report.operational.view",
        "resident.create", "resident.disable", "resident.update", "resident.view", "role.manage",
        "role.view", "tenant.manage", "tenant.view", "user.create", "user.disable", "user.update",
        "user.view", "workorder.assign", "workorder.complete", "workorder.create", "workorder.view",
    ];

    private readonly FakeTenantRepository _tenants = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeRoleRepository _roles = new();
    private readonly FakeRoleAssignmentRepository _roleAssignments = new();
    private readonly FakeRolePermissionRepository _rolePermissions = new();
    private readonly FakePermissionRepository _permissions = new();

    private ServiceProvider BuildServices()
    {
        _permissions.Permissions.AddRange(
            FullCatalogueNames.Select(n => Permission.Create(PermissionId.New(), n, n, n.Split('.')[0])));

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ITenantRepository>(_tenants);
        services.AddSingleton<IUserRepository>(_users);
        services.AddSingleton<IRoleRepository>(_roles);
        services.AddSingleton<IRoleAssignmentRepository>(_roleAssignments);
        services.AddSingleton<IRolePermissionRepository>(_rolePermissions);
        services.AddSingleton<IPermissionRepository>(_permissions);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork(_tenants, _users, _roles, _roleAssignments, _rolePermissions));
        services.AddSingleton<IClock>(new FixedClock(DateTimeOffset.UtcNow));
        services.AddSingleton<IPasswordHasher>(
            new Argon2idPasswordHasher(Options.Create(new Argon2Settings { MemoryKb = 8192, Iterations = 1, Parallelism = 1 })));
        services.AddSingleton<ISuperAdminBootstrapper, SuperAdminBootstrapper>();
        services.AddSingleton<IDefaultRoleCatalogueSeeder, DefaultRoleCatalogueSeeder>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SeedArpDevelopmentBootstrapAsync_On_A_Fresh_Database_Creates_Tenant_Users_Roles_And_Grants()
    {
        IServiceProvider services = BuildServices();

        await services.SeedArpDevelopmentBootstrapAsync(NullLogger.Instance, CancellationToken.None);

        _tenants.Tenants.Should().ContainSingle(t => t.Slug == "arp" && t.Status == TenantStatus.Active);
        Tenant tenant = _tenants.Tenants[0];

        _users.Users.Should().HaveCount(3);
        User superAdmin = _users.Users.Single(u => u.Email == "sadmin@mycondo.com");
        User admin = _users.Users.Single(u => u.Email == "admin@mycondo.com");
        User testUser = _users.Users.Single(u => u.Email == "test@mycondo.com");

        // Never the plaintext input.
        superAdmin.PasswordHash.Should().NotBe("SAdmin@1357#").And.StartWith("$argon2id$");
        admin.PasswordHash.Should().NotBe("Admin@1357#").And.StartWith("$argon2id$");
        testUser.PasswordHash.Should().NotBe("Test@1357#").And.StartWith("$argon2id$");

        _roles.Roles.Select(r => r.Name).Should().Contain(
            ["SuperAdmin", "BuildingAdmin", "Treasurer", "Secretary", "SecurityHead", "Owner", "Renter", "Auditor"]);

        Role superAdminRole = _roles.Roles.Single(r => r.Name == "SuperAdmin");
        Role buildingAdminRole = _roles.Roles.Single(r => r.Name == "BuildingAdmin");
        Role ownerRole = _roles.Roles.Single(r => r.Name == "Owner");

        _rolePermissions.Grants.Count(g => g.RoleId == superAdminRole.Id).Should().Be(_permissions.Permissions.Count);

        _roleAssignments.Assignments.Should().Contain(a => a.UserId == superAdmin.Id && a.RoleId == superAdminRole.Id);
        _roleAssignments.Assignments.Should().Contain(a => a.UserId == admin.Id && a.RoleId == buildingAdminRole.Id);
        _roleAssignments.Assignments.Should().Contain(a => a.UserId == testUser.Id && a.RoleId == ownerRole.Id);

        _roleAssignments.Assignments.Should().OnlyContain(a => a.TenantId == tenant.Id.Value);
        _users.Users.Should().OnlyContain(u => u.TenantId == tenant.Id.Value);
        _roles.Roles.Should().OnlyContain(r => r.TenantId == tenant.Id.Value);
    }

    [Fact]
    public async Task SeedArpDevelopmentBootstrapAsync_Run_Twice_Creates_No_Duplicates()
    {
        IServiceProvider services = BuildServices();

        await services.SeedArpDevelopmentBootstrapAsync(NullLogger.Instance, CancellationToken.None);
        await services.SeedArpDevelopmentBootstrapAsync(NullLogger.Instance, CancellationToken.None);

        _tenants.Tenants.Should().HaveCount(1);
        _users.Users.Should().HaveCount(3);
        _roles.Roles.Select(r => r.Name).Should().OnlyHaveUniqueItems();
        _roleAssignments.Assignments.Should().HaveCount(3);

        Role superAdminRole = _roles.Roles.Single(r => r.Name == "SuperAdmin");
        _rolePermissions.Grants.Count(g => g.RoleId == superAdminRole.Id).Should().Be(_permissions.Permissions.Count);
    }
}
