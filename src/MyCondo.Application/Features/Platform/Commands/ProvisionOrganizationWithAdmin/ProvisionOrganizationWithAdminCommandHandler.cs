using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;

/// <summary>
/// Provisions a new organization AND its founding administrator in one atomic operation — the
/// counterpart to the two-step dance the pre-existing Platform metadata-only provisioning +
/// separate tenant self-registration bootstrap otherwise required. Reuses the exact same
/// bootstrap/role-catalogue services <see cref="Auth.Commands.Register.RegisterUserCommandHandler"/>
/// already composes for "first user of a tenant" — no parallel bootstrap logic is introduced.
///
/// Writes to RLS-protected <c>identity.*</c> tables (the admin user, their role grant, the seeded
/// role catalogues) via <see cref="ITenantScopedUnitOfWork"/> rather than the ambient request-scoped
/// unit of work, because a Platform-authenticated request has no tenant JWT claim to establish RLS's
/// tenant context from (see ADR-019) — the tenant-scoped unit of work declares the brand-new tenant's
/// own id as that context, which is legitimate here because this operation is the one creating that
/// tenant. <see cref="ITenantRepository"/> reads (slug/code uniqueness) still use the ambient,
/// DI-injected repository since <c>tenancy.tenants</c> carries no RLS policy at all.
/// </summary>
public sealed class ProvisionOrganizationWithAdminCommandHandler(
    ITenantRepository tenants,
    ITenantScopedUnitOfWorkFactory tenantScopedUnitOfWorkFactory,
    IPasswordHasher passwordHasher,
    IClock clock,
    ICurrentPlatformUserProvider currentPlatformUser,
    ILoggerFactory loggerFactory,
    ILogger<ProvisionOrganizationWithAdminCommandHandler> logger
) : IRequestHandler<ProvisionOrganizationWithAdminCommand, ProvisionOrganizationResult>
{
    public async ValueTask<ProvisionOrganizationResult> Handle(
        ProvisionOrganizationWithAdminCommand command, CancellationToken cancellationToken)
    {
        string normalizedSlug = command.Slug.Trim().ToLowerInvariant();
        string normalizedCode = command.Code.Trim().ToUpperInvariant();
        string normalizedEmail = command.AdministratorEmail.Trim().ToLowerInvariant();

        if (await tenants.SlugExistsAsync(normalizedSlug, cancellationToken))
        {
            throw new ConflictException($"An organization with slug '{normalizedSlug}' already exists.");
        }

        if (await tenants.CodeExistsAsync(normalizedCode, cancellationToken))
        {
            throw new ConflictException($"An organization with code '{normalizedCode}' already exists.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        TenantId tenantId = TenantId.New();

        await using ITenantScopedUnitOfWork uow = tenantScopedUnitOfWorkFactory.Create(tenantId.Value);

        Tenant tenant = Tenant.Provision(tenantId, command.Name, normalizedSlug, nowUtc);
        tenant.UpdateDetails(command.Name, normalizedCode, nowUtc);
        tenant.Activate(nowUtc);
        uow.Tenants.Add(tenant);

        string passwordHash = passwordHasher.Hash(command.AdministratorPassword);
        User admin = User.Register(
            tenant.Id.Value, normalizedEmail, passwordHash, command.AdministratorFullName, phoneNumber: null, nowUtc);
        uow.Users.Add(admin);

        tenant.SetPrimaryAdministrator(admin.Id.Value, admin.FullName, admin.Email);

        OrganizationAdminBootstrapper organizationAdminBootstrapper = new(
            uow.Roles, uow.Permissions, uow.RolePermissions, uow.RoleAssignments,
            loggerFactory.CreateLogger<OrganizationAdminBootstrapper>());
        await organizationAdminBootstrapper.BootstrapAsync(tenant.Id.Value, admin, nowUtc, cancellationToken);

        DefaultRoleCatalogueSeeder defaultRoleCatalogueSeeder = new(
            uow.Roles, uow.Permissions, uow.RolePermissions, loggerFactory.CreateLogger<DefaultRoleCatalogueSeeder>());
        CondominiumRoleCatalogueSeeder condominiumRoleCatalogueSeeder = new(
            uow.Roles, uow.Permissions, uow.RolePermissions, loggerFactory.CreateLogger<CondominiumRoleCatalogueSeeder>());
        ResidentRoleCatalogueSeeder residentRoleCatalogueSeeder = new(
            uow.Roles, uow.Permissions, uow.RolePermissions, loggerFactory.CreateLogger<ResidentRoleCatalogueSeeder>());
        ExpenseTypeCatalogueSeeder expenseTypeCatalogueSeeder = new(
            uow.ExpenseTypes, loggerFactory.CreateLogger<ExpenseTypeCatalogueSeeder>());

        await defaultRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
        await condominiumRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
        await residentRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
        await expenseTypeCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);

        await uow.TenantModules.ReplaceForTenantAsync(
            tenant.Id.Value, command.EnabledModuleKeys, nowUtc, currentPlatformUser.PlatformUserId, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} provisioned with administrator {AdminUserId} by platform user {PlatformUserId}",
            tenant.Id, admin.Id, currentPlatformUser.PlatformUserId);

        return new ProvisionOrganizationResult(
            tenant.Id.Value, tenant.Name, tenant.Code!, tenant.Slug, tenant.Status.ToString(), admin.Id.Value);
    }
}
