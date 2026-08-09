using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Interceptors;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// Development-only bootstrap for the approved Phase-2 test organization (mycondo-docs ADR-020) —
/// "Akter Residence Park" (slug <c>arp</c>) with its first user (<c>admin@mycondo.com</c>), seeded the
/// same way any real tenant's first user would be: <see cref="IOrganizationAdminBootstrapper"/> grants
/// OrganizationAdmin, then <see cref="IDefaultRoleCatalogueSeeder"/>, <see cref="ICondominiumRoleCatalogueSeeder"/>,
/// and <see cref="IResidentRoleCatalogueSeeder"/> (Phase 3, mycondo-docs ADR-021) seed the rest of the catalogue.
///
/// Unlike <see cref="PlatformBootstrapSeeder"/>/<see cref="DevelopmentTenantSeeder"/> (which write to
/// tables with no RLS at all — <c>platform.*</c> and <c>tenancy.tenants</c> respectively), this seeder
/// writes rows to RLS-protected, tenant-scoped tables (<c>identity.users/roles/role_permissions/
/// role_assignments</c>) from a hosted service's own DI scope, which has no HTTP request to resolve a
/// tenant context from — the API's real <c>ITenantContextAccessor</c> (<c>MyCondo.Api.Authentication.
/// TenantContextAccessor</c>) only ever reads a JWT claim or an anonymous-endpoint's stashed
/// <c>HttpContext.Items</c> value, neither of which exists here. Rather than adding a background-write
/// fallback to that shared, security-sensitive class, this seeder builds its own short-lived
/// <see cref="MyCondoDbContext"/> bound to a private, fixed-tenant <see cref="ITenantContextAccessor"/>
/// — the same technique <c>MyCondo.DbMigrator</c>'s production bootstrap tool and the test suites'
/// <c>PostgresApiFactory.CreateDbContextForTenant</c> already use for the identical problem. RLS is
/// fully in force for every write this seeder makes; it isn't bypassed, only correctly told which
/// tenant it's writing as.
///
/// Idempotent on the ARP tenant specifically (<see cref="ITenantRepository.SlugExistsAsync"/>), not on
/// "any tenant exists" — unlike <see cref="DevelopmentTenantSeeder"/>'s generic "demo" tenant, ARP must
/// still be created even if some other tenant already exists (e.g. on a database that already has
/// "demo" from a prior run). Registered before <c>DevelopmentTenantSeeder</c> in Program.cs so ARP
/// becomes the canonical local-dev tenant; if ARP already exists, <c>DevelopmentTenantSeeder</c>'s own
/// "any tenant exists" check correctly skips creating "demo" as well — that's a harmless, expected
/// interaction, not a conflict.
///
/// Deliberately does NOT touch the Platform tier: <see cref="PlatformBootstrapSeeder"/>'s
/// <c>sadmin@mycondo.com</c> is never attached to ARP or any other tenant.
/// </summary>
public sealed class ArpDevelopmentBootstrapSeeder(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory
) : IHostedService
{
    private const string TenantName = "Akter Residence Park";
    private const string TenantSlug = "arp";
    private const string AdminEmail = "admin@mycondo.com";
    private const string AdminPassword = "Admin@1357#";
    private const string AdminFullName = "Admin";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        ITenantRepository readOnlyTenants = sp.GetRequiredService<ITenantRepository>();
        bool arpExists = await readOnlyTenants.SlugExistsAsync(TenantSlug, cancellationToken);
        if (arpExists)
        {
            return;
        }

        IClock clock = sp.GetRequiredService<IClock>();
        IPasswordHasher passwordHasher = sp.GetRequiredService<IPasswordHasher>();
        IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
        string connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        DateTimeOffset nowUtc = clock.UtcNow;
        Tenant tenant = Tenant.Provision(TenantName, TenantSlug, nowUtc);
        tenant.Activate(nowUtc);

        // Every write below belongs to the tenant just provisioned — a fixed accessor, not the
        // request-bound one the running API otherwise uses, since there is no request here. See this
        // class's doc comment for why a shared production accessor isn't touched instead.
        FixedTenantContextAccessor tenantAccessor = new(tenant.Id.Value);

        await using MyCondoDbContext db = BuildDbContext(sp, connectionString, tenantAccessor);

        TenantRepository tenants = new(db);
        tenants.Add(tenant);

        string passwordHash = passwordHasher.Hash(AdminPassword);
        User admin = User.Register(tenant.Id.Value, AdminEmail, passwordHash, AdminFullName, phoneNumber: null, nowUtc);

        UserRepository users = new(db);
        users.Add(admin);

        RoleRepository roles = new(db);
        PermissionRepository permissions = new(db);
        RolePermissionRepository rolePermissions = new(db);
        RoleAssignmentRepository roleAssignments = new(db);

        OrganizationAdminBootstrapper organizationAdminBootstrapper = new(
            roles, permissions, rolePermissions, roleAssignments, loggerFactory.CreateLogger<OrganizationAdminBootstrapper>());
        DefaultRoleCatalogueSeeder defaultRoleCatalogueSeeder = new(
            roles, permissions, rolePermissions, loggerFactory.CreateLogger<DefaultRoleCatalogueSeeder>());
        CondominiumRoleCatalogueSeeder condominiumRoleCatalogueSeeder = new(
            roles, permissions, rolePermissions, loggerFactory.CreateLogger<CondominiumRoleCatalogueSeeder>());
        ResidentRoleCatalogueSeeder residentRoleCatalogueSeeder = new(
            roles, permissions, rolePermissions, loggerFactory.CreateLogger<ResidentRoleCatalogueSeeder>());

        await organizationAdminBootstrapper.BootstrapAsync(tenant.Id.Value, admin, nowUtc, cancellationToken);
        await defaultRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
        await condominiumRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
        await residentRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        loggerFactory.CreateLogger<ArpDevelopmentBootstrapSeeder>().LogInformation(
            "Development seed: provisioned tenant {TenantId} ('{TenantName}', slug '{Slug}') with OrganizationAdmin {AdminUserId} ({AdminEmail})",
            tenant.Id, tenant.Name, tenant.Slug, admin.Id, admin.Email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static MyCondoDbContext BuildDbContext(
        IServiceProvider sp, string connectionString, ITenantContextAccessor tenantAccessor)
    {
        TenantContextConnectionInterceptor tenantInterceptor = new(tenantAccessor);

        DbContextOptions<MyCondoDbContext> options = new DbContextOptionsBuilder<MyCondoDbContext>()
            .UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<DispatchDomainEventsInterceptor>(),
                tenantInterceptor)
            .Options;

        return new MyCondoDbContext(options);
    }

    private sealed class FixedTenantContextAccessor(Guid tenantId) : ITenantContextAccessor
    {
        public Guid? CurrentTenantId => tenantId;
    }
}
