using Microsoft.EntityFrameworkCore;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;

namespace MyCondo.Infrastructure.Persistence;

public sealed class MyCondoDbContext(
    DbContextOptions<MyCondoDbContext> options,
    ITenantContextAccessor tenantContext
) : DbContext(options), IUnitOfWork
{
    private readonly ITenantContextAccessor _tenantContext = tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema-per-module: each module's IEntityTypeConfiguration<T> sets ToTable(name, schema: "<module>").
        // No HasDefaultSchema() — every aggregate must declare its schema explicitly to avoid silent fallthrough.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyCondoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Push the current tenant onto the connection so PostgreSQL Row-Level Security
        // policies (`tenant_id = current_setting('app.current_tenant_id')::uuid`) enforce isolation.
        // RLS is ENABLED + FORCED on every tenant-scoped table — application bypass is impossible.
        await SetTenantContextAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task SetTenantContextAsync(CancellationToken cancellationToken)
    {
        Guid? tenantId = _tenantContext.CurrentTenantId;
        if (tenantId is null)
        {
            // System-level operations (migrations, seed, super-admin tenant provisioning) run without a tenant.
            // RLS policies must explicitly handle the unset case — typically by returning no rows.
            return;
        }

        await Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_tenant_id', {0}, false);",
            new object[] { tenantId.Value.ToString() },
            cancellationToken);
    }
}
