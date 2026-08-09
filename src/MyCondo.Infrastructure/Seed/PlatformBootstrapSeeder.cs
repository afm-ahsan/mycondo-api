using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Platform.PlatformRolePermissions;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// Development-only bootstrap for the Platform SuperAdmin — the Platform-scope analogue of
/// <see cref="DevelopmentTenantSeeder"/>, deliberately kept as a separate, unrelated seeder: this
/// seeder has no knowledge of Tenant/Organization concepts, and DevelopmentTenantSeeder has no
/// knowledge of Platform identities. See mycondo-docs ADR-019 and the approved Phase 1 blueprint §14
/// ("the platform account and tenant account must be seeded through separate, idempotent seed paths").
/// Invoked explicitly by <c>DatabaseSeederExtensions.SeedDatabaseAsync</c>, not registered as an
/// <c>IHostedService</c> — see that class for the Development-only guard.
///
/// The SuperAdmin identity itself is a true singleton, guarded by "does a PlatformUser with this email
/// already exist" — but the SuperAdmin PlatformRole's permission grants are reconciled on every call,
/// not just at first creation, so a <c>platform.*</c> permission added to the catalogue later still
/// reaches an already-bootstrapped SuperAdmin (ADR-019 called the old all-or-nothing version of this
/// seeder "idempotent," but that described a full no-op on rerun, not reconciliation — see the ADR
/// entry superseding that claim).
/// </summary>
public sealed class PlatformBootstrapSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<PlatformBootstrapSeeder> logger
)
{
    private const string SuperAdminEmail = "sadmin@mycondo.com";
    private const string SuperAdminPassword = "SAdmin@1357#";
    private const string SuperAdminDisplayName = "Platform SuperAdmin";
    private const string SuperAdminRoleName = "SuperAdmin";
    // Lowercase, matching every other permission Module value in the catalog (see
    // PermissionCatalogue.cs: "tenant", "user", "property", etc.) — the Permission.Module comparison
    // is a plain case-sensitive string equality translated to SQL.
    private const string PlatformPermissionModule = "platform";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        IPlatformUserRepository platformUsers = sp.GetRequiredService<IPlatformUserRepository>();
        IPlatformRoleRepository platformRoles = sp.GetRequiredService<IPlatformRoleRepository>();
        IPlatformRolePermissionRepository platformRolePermissions =
            sp.GetRequiredService<IPlatformRolePermissionRepository>();
        IPlatformUserRoleAssignmentRepository platformAssignments =
            sp.GetRequiredService<IPlatformUserRoleAssignmentRepository>();
        IPermissionRepository permissions = sp.GetRequiredService<IPermissionRepository>();
        IPasswordHasher passwordHasher = sp.GetRequiredService<IPasswordHasher>();
        IUnitOfWork unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        IClock clock = sp.GetRequiredService<IClock>();

        DateTimeOffset nowUtc = clock.UtcNow;

        PlatformUser? existingSuperAdmin = await platformUsers.GetByEmailAsync(SuperAdminEmail, cancellationToken);
        PlatformRole? superAdminRole = await platformRoles.GetByNameAsync(SuperAdminRoleName, cancellationToken);

        bool roleCreated = false;
        if (superAdminRole is null)
        {
            superAdminRole = PlatformRole.CreateSystem(
                PlatformRoleId.New(),
                SuperAdminRoleName,
                "Full access to all Platform-scope permissions (development bootstrap).",
                nowUtc);
            platformRoles.Add(superAdminRole);
            roleCreated = true;
        }

        // Reconciled every run, regardless of whether the role/user already existed — a platform.*
        // permission added to the catalogue after this seeder first ran must still reach SuperAdmin.
        List<Permission> platformPermissions =
            await permissions.GetByModuleAsync(PlatformPermissionModule, cancellationToken);
        HashSet<string> grantedNames = roleCreated
            ? []
            : (await platformRolePermissions.GetPermissionNamesForRoleAsync(superAdminRole.Id, cancellationToken))
                .ToHashSet(StringComparer.Ordinal);
        int existingGrantCount = grantedNames.Count;

        int grantsCreated = 0;
        foreach (Permission permission in platformPermissions)
        {
            if (grantedNames.Add(permission.Name))
            {
                platformRolePermissions.Add(
                    new PlatformRolePermission(superAdminRole.Id, permission.Id, nowUtc, grantedBy: null));
                grantsCreated++;
            }
        }

        bool userCreated = existingSuperAdmin is null;
        if (userCreated)
        {
            string passwordHash = passwordHasher.Hash(SuperAdminPassword);
            PlatformUser superAdmin = PlatformUser.Create(SuperAdminEmail, passwordHash, SuperAdminDisplayName, nowUtc);
            platformUsers.Add(superAdmin);
            platformAssignments.Add(PlatformUserRoleAssignment.Grant(superAdmin.Id, superAdminRole.Id, nowUtc));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[DatabaseSeed] Platform SuperAdmin: {UserStatus}, {RoleStatus}; {Expected} platform " +
            "permissions expected, {Created} grants created, {Existing} existing",
            userCreated ? "created" : "existing", roleCreated ? "role created" : "role existing",
            platformPermissions.Count, grantsCreated, existingGrantCount);
    }
}
