using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
/// role_assignments</c>) from its own DI scope, which has no HTTP request to resolve a tenant context
/// from — the API's real <c>ITenantContextAccessor</c> (<c>MyCondo.Api.Authentication.
/// TenantContextAccessor</c>) only ever reads a JWT claim or an anonymous-endpoint's stashed
/// <c>HttpContext.Items</c> value, neither of which exists here. Rather than adding a background-write
/// fallback to that shared, security-sensitive class, this seeder builds its own short-lived
/// <see cref="MyCondoDbContext"/> bound to a private, fixed-tenant <see cref="ITenantContextAccessor"/>
/// — the same technique <c>MyCondo.DbMigrator</c>'s production bootstrap tool and the test suites'
/// <c>PostgresApiFactory.CreateDbContextForTenant</c> already use for the identical problem. RLS is
/// fully in force for every write this seeder makes; it isn't bypassed, only correctly told which
/// tenant it's writing as.
///
/// Tenant/admin-user/OrganizationAdmin creation is a one-time bootstrap event, guarded by
/// <see cref="ITenantRepository.SlugExistsAsync"/> on the ARP tenant specifically (not on "any tenant
/// exists" — unlike <see cref="DevelopmentTenantSeeder"/>'s generic "demo" tenant, ARP must still be
/// created even if some other tenant already exists). The three role-catalogue seeders, however, run
/// on *every* call regardless of whether ARP was just created or already existed — they're reconciled
/// by natural key (see <see cref="DefaultRoleCatalogueSeeder"/> etc.), so a role or permission added to
/// one of those catalogues after ARP already exists still reaches it on the next app startup instead of
/// being silently skipped forever.
///
/// Invoked before <c>DevelopmentTenantSeeder</c> by <c>DatabaseSeederExtensions.SeedDatabaseAsync</c>
/// so ARP becomes the canonical local-dev tenant; if ARP already exists, <c>DevelopmentTenantSeeder</c>'s
/// own "any tenant exists" check correctly skips creating "demo" as well — that's a harmless, expected
/// interaction, not a conflict.
///
/// Deliberately does NOT touch the Platform tier: <see cref="PlatformBootstrapSeeder"/>'s
/// <c>sadmin@mycondo.com</c> is never attached to ARP or any other tenant.
/// </summary>
public sealed class ArpDevelopmentBootstrapSeeder(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory
)
{
    private const string TenantName = "Akter Residence Park";
    private const string TenantSlug = "arp";
    private const string AdminEmail = "admin@mycondo.com";
    private const string AdminPassword = "Admin@1357#";
    private const string AdminFullName = "Admin";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        ITenantRepository readOnlyTenants = sp.GetRequiredService<ITenantRepository>();
        IClock clock = sp.GetRequiredService<IClock>();
        IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
        string connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        DateTimeOffset nowUtc = clock.UtcNow;

        Tenant? existingTenant = await readOnlyTenants.GetBySlugAsync(TenantSlug, cancellationToken);
        bool tenantCreated = existingTenant is null;
        Guid tenantId = existingTenant?.Id.Value ?? Guid.Empty; // resolved inside CreateTenantAndAdminAsync when created

        // Every write below belongs to the ARP tenant — a fixed accessor, not the request-bound one
        // the running API otherwise uses, since there is no request here. See this class's doc comment
        // for why a shared production accessor isn't touched instead.
        if (tenantCreated)
        {
            // First-time bootstrap: tenant + admin + OrganizationAdmin + all three role catalogues are
            // one logical unit, committed in a single transaction (spec §13) — nothing about ARP is
            // half-created if this fails partway through.
            tenantId = await CreateTenantAdminAndCataloguesAsync(sp, connectionString, nowUtc, cancellationToken);
        }
        else
        {
            // ARP already exists: only reconcile the role catalogues, in their own transaction — a
            // role/permission added to a catalogue after ARP was first created still reaches it here.
            await ReconcileRoleCataloguesAsync(sp, connectionString, tenantId, nowUtc, cancellationToken);
        }

        loggerFactory.CreateLogger<ArpDevelopmentBootstrapSeeder>().LogInformation(
            "[DatabaseSeed] ARP tenant: {TenantStatus} (tenant {TenantId}, slug '{Slug}')",
            tenantCreated ? "created" : "existing", tenantId, TenantSlug);
    }

    private static async Task<Guid> CreateTenantAdminAndCataloguesAsync(
        IServiceProvider sp, string connectionString, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        IPasswordHasher passwordHasher = sp.GetRequiredService<IPasswordHasher>();

        Tenant tenant = Tenant.Provision(TenantName, TenantSlug, nowUtc);
        tenant.Activate(nowUtc);

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
        ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        OrganizationAdminBootstrapper organizationAdminBootstrapper = new(
            roles, permissions, rolePermissions, roleAssignments,
            loggerFactory.CreateLogger<OrganizationAdminBootstrapper>());
        await organizationAdminBootstrapper.BootstrapAsync(tenant.Id.Value, admin, nowUtc, cancellationToken);

        await SeedRoleCataloguesAsync(roles, permissions, rolePermissions, loggerFactory, tenant.Id.Value, nowUtc, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return tenant.Id.Value;
    }

    private static async Task ReconcileRoleCataloguesAsync(
        IServiceProvider sp, string connectionString, Guid tenantId, DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        FixedTenantContextAccessor tenantAccessor = new(tenantId);
        await using MyCondoDbContext db = BuildDbContext(sp, connectionString, tenantAccessor);

        RoleRepository roles = new(db);
        PermissionRepository permissions = new(db);
        RolePermissionRepository rolePermissions = new(db);
        ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        await SeedRoleCataloguesAsync(roles, permissions, rolePermissions, loggerFactory, tenantId, nowUtc, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRoleCataloguesAsync(
        RoleRepository roles, PermissionRepository permissions, RolePermissionRepository rolePermissions,
        ILoggerFactory loggerFactory, Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        DefaultRoleCatalogueSeeder defaultRoleCatalogueSeeder = new(
            roles, permissions, rolePermissions, loggerFactory.CreateLogger<DefaultRoleCatalogueSeeder>());
        CondominiumRoleCatalogueSeeder condominiumRoleCatalogueSeeder = new(
            roles, permissions, rolePermissions, loggerFactory.CreateLogger<CondominiumRoleCatalogueSeeder>());
        ResidentRoleCatalogueSeeder residentRoleCatalogueSeeder = new(
            roles, permissions, rolePermissions, loggerFactory.CreateLogger<ResidentRoleCatalogueSeeder>());

        await defaultRoleCatalogueSeeder.SeedAsync(tenantId, nowUtc, cancellationToken);
        await condominiumRoleCatalogueSeeder.SeedAsync(tenantId, nowUtc, cancellationToken);
        await residentRoleCatalogueSeeder.SeedAsync(tenantId, nowUtc, cancellationToken);
    }

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
