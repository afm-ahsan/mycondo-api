using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyCondo.Application;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Identity;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Interceptors;
using MyCondo.Infrastructure.Persistence.Repositories;
using MyCondo.Infrastructure.Time;

namespace MyCondo.DbMigrator;

/// <summary>
/// Standalone production tenant-bootstrap tool (ADR-015, resolving MASTER_BACKLOG.md MT-1a). Refuses
/// to run if any tenant already exists anywhere — this environment gets exactly one first tenant,
/// created explicitly by an operator, never by an anonymous HTTP call. Local dev keeps using
/// `DevelopmentTenantSeeder`, which this tool is unrelated to.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "bootstrap", StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            return 1;
        }

        Dictionary<string, string> options = ParseOptions(args.AsSpan(1).ToArray());

        if (!options.TryGetValue("tenant-name", out string? tenantName)
            || !options.TryGetValue("tenant-slug", out string? tenantSlug)
            || !options.TryGetValue("admin-email", out string? adminEmail))
        {
            Console.Error.WriteLine("Missing required options. See usage below.");
            PrintUsage();
            return 1;
        }

        string adminName = options.GetValueOrDefault("admin-name", "System Administrator");

        string connectionString = options.GetValueOrDefault("connection-string")
            ?? Environment.GetEnvironmentVariable("MYCONDO_DB_CONNECTION_STRING")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine(
                "No connection string. Pass --connection-string or set MYCONDO_DB_CONNECTION_STRING.");
            return 1;
        }

        string adminPassword = ResolveAdminPassword(options);
        if (string.IsNullOrEmpty(adminPassword))
        {
            Console.Error.WriteLine("Admin password was empty. Aborting.");
            return 1;
        }

        ServiceCollection services = new();
        AmbientTenantContextAccessor tenantContext = new();
        ConfigureServices(services, connectionString, tenantContext);

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        ILogger logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("MyCondo.DbMigrator");

        // The global permission catalogue is system-wide reference data (identity.permissions has no
        // tenant_id/RLS) — reconciled here, unconditionally, before the first tenant's role catalogues
        // are seeded below, since those resolve permission names against this table. Safe to run even
        // if the API has already seeded permissions on this same database (reconciled by Name).
        IPermissionSeeder permissionSeeder = sp.GetRequiredService<IPermissionSeeder>();
        await permissionSeeder.SeedAsync(CancellationToken.None);
        // Flushed immediately, separately from the tenant-bootstrap SaveChanges below — the role
        // catalogue seeders query permissions by Name via a fresh (server-side) query, which would not
        // see a just-added-but-unsaved permission row otherwise.
        await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        ITenantRepository tenants = sp.GetRequiredService<ITenantRepository>();
        bool anyTenantExists = await tenants.AnyAsync(CancellationToken.None);
        if (anyTenantExists)
        {
            logger.LogInformation(
                "Bootstrap skipped: a tenant already exists. This tool only ever creates the first one.");
            return 0;
        }

        IClock clock = sp.GetRequiredService<IClock>();
        DateTimeOffset nowUtc = clock.UtcNow;

        Tenant tenant = Tenant.Provision(tenantName, tenantSlug, nowUtc);
        tenant.Activate(nowUtc);
        tenants.Add(tenant);

        // From here on, every write below belongs to the tenant we just created — set it once so
        // TenantContextConnectionInterceptor's RLS GUC matches on every connection this run opens.
        tenantContext.CurrentTenantId = tenant.Id.Value;

        IPasswordHasher passwordHasher = sp.GetRequiredService<IPasswordHasher>();
        string passwordHash = passwordHasher.Hash(adminPassword);

        User admin = User.Register(
            tenant.Id.Value,
            adminEmail.Trim().ToLowerInvariant(),
            passwordHash,
            adminName,
            phoneNumber: null,
            nowUtc);

        IUserRepository users = sp.GetRequiredService<IUserRepository>();
        users.Add(admin);

        IOrganizationAdminBootstrapper bootstrapper = sp.GetRequiredService<IOrganizationAdminBootstrapper>();
        await bootstrapper.BootstrapAsync(tenant.Id.Value, admin, nowUtc, CancellationToken.None);

        IDefaultRoleCatalogueSeeder defaultRoleCatalogueSeeder = sp.GetRequiredService<IDefaultRoleCatalogueSeeder>();
        await defaultRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, CancellationToken.None);

        ICondominiumRoleCatalogueSeeder condominiumRoleCatalogueSeeder = sp.GetRequiredService<ICondominiumRoleCatalogueSeeder>();
        await condominiumRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, CancellationToken.None);

        IResidentRoleCatalogueSeeder residentRoleCatalogueSeeder = sp.GetRequiredService<IResidentRoleCatalogueSeeder>();
        await residentRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, CancellationToken.None);

        IUnitOfWork unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Structured audit trail for this operation. No dedicated AuditLog table exists yet (a
        // later-wave concern) — this log line is the provisional record. Never logs the password.
        logger.LogInformation(
            "Production tenant bootstrap: tenant {TenantId} (slug '{TenantSlug}') created with " +
            "admin user {AdminUserId} ({AdminEmail}) by operator '{OperatorUser}' at {TimestampUtc}",
            tenant.Id, tenant.Slug, admin.Id, admin.Email, Environment.UserName, nowUtc);

        return 0;
    }

    private static void ConfigureServices(
        ServiceCollection services, string connectionString, AmbientTenantContextAccessor tenantContext)
    {
        services.AddLogging(builder => builder.AddConsole());
        services.AddApplication();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICurrentUserProvider, NullCurrentUserProvider>();
        services.AddSingleton<ITenantContextAccessor>(tenantContext);

        services.AddOptions<Argon2Settings>();
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddScoped<TenantContextConnectionInterceptor>();

        services.AddDbContext<MyCondoDbContext>((sp, dbOptions) =>
        {
            dbOptions.UseNpgsql(connectionString, npg =>
                    npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<DispatchDomainEventsInterceptor>(),
                    sp.GetRequiredService<TenantContextConnectionInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MyCondoDbContext>());

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
    }

    private static string ResolveAdminPassword(Dictionary<string, string> options)
    {
        if (options.TryGetValue("admin-password-env", out string? envVarName))
        {
            return Environment.GetEnvironmentVariable(envVarName) ?? string.Empty;
        }

        return ReadPasswordMasked();
    }

    private static string ReadPasswordMasked()
    {
        Console.Write("Admin password: ");
        StringBuilder builder = new();
        ConsoleKeyInfo key;

        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                if (builder.Length > 0)
                {
                    builder.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                builder.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return builder.ToString();
    }

    private static Dictionary<string, string> ParseOptions(string[] remainingArgs)
    {
        Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < remainingArgs.Length; i++)
        {
            string arg = remainingArgs[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = arg[2..];
            string value = i + 1 < remainingArgs.Length ? remainingArgs[++i] : string.Empty;
            options[key] = value;
        }

        return options;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine(
            """
            MyCondo.DbMigrator bootstrap — creates the first tenant and its OrganizationAdmin.
            Refuses (idempotent no-op) if any tenant already exists anywhere.

            Usage:
              MyCondo.DbMigrator bootstrap
                --tenant-name <name>
                --tenant-slug <slug>
                --admin-email <email>
                [--admin-name <name>]                 (default: "System Administrator")
                [--admin-password-env <ENV_VAR_NAME>] (default: masked interactive prompt)
                [--connection-string <connection string>] (default: MYCONDO_DB_CONNECTION_STRING)
            """);
    }
}
