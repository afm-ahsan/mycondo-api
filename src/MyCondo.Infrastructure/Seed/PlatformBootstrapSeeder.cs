using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
/// <see cref="DevelopmentTenantSeeder"/>, deliberately kept as a separate, unrelated hosted service:
/// this seeder has no knowledge of Tenant/Organization concepts, and DevelopmentTenantSeeder has no
/// knowledge of Platform identities. See mycondo-docs ADR-019 and the approved Phase 1 blueprint §14
/// ("the platform account and tenant account must be seeded through separate, idempotent seed paths").
///
/// Idempotent the same way DevelopmentTenantSeeder is: if any PlatformUser already exists, this is a
/// no-op — running it repeatedly never duplicates the PlatformRole, its permission grants, the
/// PlatformUser, or the PlatformUserRoleAssignment.
/// </summary>
public sealed class PlatformBootstrapSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<PlatformBootstrapSeeder> logger
) : IHostedService
{
    private const string SuperAdminEmail = "sadmin@mycondo.com";
    private const string SuperAdminPassword = "SAdmin@1357#";
    private const string SuperAdminDisplayName = "Platform SuperAdmin";
    private const string SuperAdminRoleName = "SuperAdmin";
    // Lowercase, matching every other permission Module value in the catalog (see
    // Seed_Permission_Catalogue.cs: "tenant", "user", "property", etc.) — the Permission.Module
    // comparison is a plain case-sensitive string equality translated to SQL.
    private const string PlatformPermissionModule = "platform";

    public async Task StartAsync(CancellationToken cancellationToken)
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

        bool anyPlatformUserExists = await platformUsers.AnyAsync(cancellationToken);
        if (anyPlatformUserExists)
        {
            return;
        }

        DateTimeOffset nowUtc = clock.UtcNow;

        PlatformRole superAdminRole = PlatformRole.CreateSystem(
            PlatformRoleId.New(),
            SuperAdminRoleName,
            "Full access to all Platform-scope permissions (development bootstrap).",
            nowUtc);
        platformRoles.Add(superAdminRole);

        List<Permission> platformPermissions =
            await permissions.GetByModuleAsync(PlatformPermissionModule, cancellationToken);
        foreach (Permission permission in platformPermissions)
        {
            platformRolePermissions.Add(
                new PlatformRolePermission(superAdminRole.Id, permission.Id, nowUtc, grantedBy: null));
        }

        string passwordHash = passwordHasher.Hash(SuperAdminPassword);
        PlatformUser superAdmin = PlatformUser.Create(
            SuperAdminEmail, passwordHash, SuperAdminDisplayName, nowUtc);
        platformUsers.Add(superAdmin);

        platformAssignments.Add(
            PlatformUserRoleAssignment.Grant(superAdmin.Id, superAdminRole.Id, nowUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Development seed: provisioned Platform SuperAdmin {PlatformUserId} ({Email}) with {PermissionCount} permissions — no tenant membership",
            superAdmin.Id, superAdmin.Email, platformPermissions.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
