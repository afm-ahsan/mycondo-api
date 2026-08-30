using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using Npgsql;

namespace MyCondo.Infrastructure.Persistence;

public sealed class MyCondoDbContext(
    DbContextOptions<MyCondoDbContext> options
) : DbContext(options), IUnitOfWork
{
    // The two Task 14A subscription-invoice uniqueness constraints are the durable idempotency backstop
    // for manual invoice generation (ADR-034 Task 14B) — translated here, at the one place every save
    // path already funnels through, so callers get ConflictException instead of a raw PostgreSQL
    // unique-violation. Scoped to exactly these constraint names so every other unique-index violation
    // in the app keeps its existing (untranslated) behavior.
    private static readonly HashSet<string> DuplicateSubscriptionInvoiceConstraints =
    [
        "ux_subscription_invoices_subscription_id_period",
        "ux_subscription_invoices_tenant_id_invoice_number"
    ];

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema-per-module: each module's IEntityTypeConfiguration<T> sets ToTable(name, schema: "<module>").
        // No HasDefaultSchema() — every aggregate must declare its schema explicitly to avoid silent fallthrough.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyCondoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            } pg
            && pg.ConstraintName is not null
            && DuplicateSubscriptionInvoiceConstraints.Contains(pg.ConstraintName))
        {
            throw new ConflictException(
                "A subscription invoice already exists for this organization subscription and billing period, or this invoice number is already in use.");
        }
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        IDbContextTransaction transaction = await Database.BeginTransactionAsync(ct);
        return new EfUnitOfWorkTransaction(transaction);
    }

    private sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => transaction.CommitAsync(ct);

        public Task RollbackAsync(CancellationToken ct = default) => transaction.RollbackAsync(ct);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
