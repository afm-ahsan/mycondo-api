using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence.Seeding.Models;

namespace MyCondo.Infrastructure.Persistence.Seeding.Extensions;

/// <summary>
/// Orchestrates the ARP local-development bootstrap dataset (mycondo-seed-data-architecture-refactor
/// -v2.md): one active tenant ("Akter Residence Park" / slug "arp"), a tenant-scoped SuperAdmin, a
/// BuildingAdmin ("Tenant Admin"), and a low-privilege TestOwner. Every step is idempotent — see the
/// individual extension methods — so re-running this on a database that already has the dataset is a
/// no-op past the initial existence checks. Development/test only; see <see cref="DatabaseSeeder"/>
/// for the environment gate.
/// </summary>
internal static class DevelopmentSeedExtensions
{
    public static async Task SeedArpDevelopmentBootstrapAsync(
        this IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ITenantRepository tenants = services.GetRequiredService<ITenantRepository>();
        IUserRepository users = services.GetRequiredService<IUserRepository>();
        IRoleRepository roles = services.GetRequiredService<IRoleRepository>();
        IRoleAssignmentRepository roleAssignments = services.GetRequiredService<IRoleAssignmentRepository>();
        ISuperAdminBootstrapper superAdminBootstrapper = services.GetRequiredService<ISuperAdminBootstrapper>();
        IDefaultRoleCatalogueSeeder defaultRoleCatalogueSeeder = services.GetRequiredService<IDefaultRoleCatalogueSeeder>();
        IPasswordHasher passwordHasher = services.GetRequiredService<IPasswordHasher>();
        IUnitOfWork unitOfWork = services.GetRequiredService<IUnitOfWork>();
        IClock clock = services.GetRequiredService<IClock>();

        Tenant tenant = await tenants.EnsureTenantAsync(
            DevelopmentBootstrapConstants.TenantName,
            DevelopmentBootstrapConstants.TenantSlug,
            clock,
            cancellationToken);

        // Every write from here on belongs to this tenant — see AmbientTenantScope's doc comment for
        // why a startup hosted service needs this instead of the JWT/HTTP-request fallbacks
        // TenantContextAccessor normally uses.
        using IDisposable tenantScope = AmbientTenantScope.Begin(tenant.Id.Value);

        (User superAdmin, bool superAdminCreated) = await users.EnsureUserAsync(
            tenant.Id.Value,
            DevelopmentBootstrapConstants.SuperAdminEmail,
            DevelopmentBootstrapConstants.SuperAdminFullName,
            DevelopmentBootstrapConstants.SuperAdminPassword,
            passwordHasher,
            clock,
            cancellationToken);

        await roles.EnsureSuperAdminAsync(
            roleAssignments, superAdminBootstrapper, tenant.Id.Value, superAdmin, clock, cancellationToken);

        await roles.EnsureDefaultRoleCatalogueAsync(
            defaultRoleCatalogueSeeder, tenant.Id.Value, DevelopmentBootstrapConstants.AdminRoleName, clock, cancellationToken);

        // Flush here: EnsureRoleAssignmentAsync below re-queries roles by name (a real SELECT, not the
        // change tracker), so BuildingAdmin/Owner — just staged in-memory by
        // EnsureDefaultRoleCatalogueAsync — need to actually exist in the database first. Splitting
        // into two SaveChanges calls doesn't reintroduce partial-provisioning risk (standard #9):
        // every step above and below is independently idempotent, so re-running this whole method
        // after a failure between the two calls safely picks up wherever it left off.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        (User admin, _) = await users.EnsureUserAsync(
            tenant.Id.Value,
            DevelopmentBootstrapConstants.AdminEmail,
            DevelopmentBootstrapConstants.AdminFullName,
            DevelopmentBootstrapConstants.AdminPassword,
            passwordHasher,
            clock,
            cancellationToken);

        await roles.EnsureRoleAssignmentAsync(
            roleAssignments, tenant.Id.Value, admin, DevelopmentBootstrapConstants.AdminRoleName, clock, cancellationToken);

        (User testUser, _) = await users.EnsureUserAsync(
            tenant.Id.Value,
            DevelopmentBootstrapConstants.TestUserEmail,
            DevelopmentBootstrapConstants.TestUserFullName,
            DevelopmentBootstrapConstants.TestUserPassword,
            passwordHasher,
            clock,
            cancellationToken);

        await roles.EnsureRoleAssignmentAsync(
            roleAssignments, tenant.Id.Value, testUser, DevelopmentBootstrapConstants.TestUserRoleName, clock, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Never logs a password — only ids/emails, matching the ISuperAdminBootstrapper/DbMigrator
        // logging convention.
        logger.LogInformation(
            "Development bootstrap ensured for tenant '{TenantSlug}' ({TenantId}): SuperAdmin {SuperAdminEmail} " +
            "(created={SuperAdminCreated}), Admin {AdminEmail}, TestUser {TestUserEmail}",
            tenant.Slug, tenant.Id, superAdmin.Email, superAdminCreated, admin.Email, testUser.Email);
    }
}
