using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Domain.Abstractions;

/// <summary>
/// A short-lived unit of work whose underlying database connection has an explicit, fixed tenant
/// context declared for its whole lifetime — needed by Platform-tier operations that must write to
/// RLS-protected, tenant-scoped tables (<c>identity.*</c>) on behalf of a tenant with no ambient
/// JWT-derived tenant claim to establish that context from (Platform-authenticated requests
/// structurally carry no <c>tenant_id</c> claim — see ADR-019). This is the same technique
/// <c>ArpDevelopmentBootstrapSeeder</c>/<c>MyCondo.DbMigrator</c>/test fixtures already use for the
/// identical problem outside an HTTP request; this abstraction lets a real Mediator command handler
/// (e.g. organization provisioning) use it too, without the Application layer depending on
/// Infrastructure/EF Core directly. Distinct from the request-scoped <see cref="IUnitOfWork"/>, which
/// relies on the caller's own authenticated tenant context and must never be used for a tenant other
/// than the one the caller is authenticated against.
/// </summary>
public interface ITenantScopedUnitOfWork : IAsyncDisposable
{
    ITenantRepository Tenants { get; }
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IPermissionRepository Permissions { get; }
    IRolePermissionRepository RolePermissions { get; }
    IRoleAssignmentRepository RoleAssignments { get; }
    ITenantModuleRepository TenantModules { get; }
    IExpenseTypeRepository ExpenseTypes { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ITenantScopedUnitOfWorkFactory
{
    /// <summary>Creates a unit of work whose DB connection reports <paramref name="tenantId"/> as
    /// its RLS tenant context for every write made through it. Callers must only ever pass a tenant
    /// id the calling operation itself is legitimately creating/administering — never a value derived
    /// from untrusted input naming a different, pre-existing tenant.</summary>
    ITenantScopedUnitOfWork Create(Guid tenantId);
}
