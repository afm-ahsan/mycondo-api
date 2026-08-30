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
    // Platform SaaS billing's uniqueness constraints (Task 14A/14B subscription-invoice constraints,
    // plus the Task 14C subscription-payment reference constraint) are the durable idempotency backstop
    // for manual invoice generation and payment recording — translated here, at the one place every save
    // path already funnels through, so callers get ConflictException instead of a raw PostgreSQL
    // unique-violation. Scoped to exactly these constraint names so every other unique-index violation
    // in the app keeps its existing (untranslated) behavior.
    private static readonly Dictionary<string, string> DuplicatePlatformBillingConstraints = new()
    {
        ["ux_subscription_invoices_subscription_id_period"] =
            "A subscription invoice already exists for this organization subscription and billing period, or this invoice number is already in use.",
        ["ux_subscription_invoices_tenant_id_invoice_number"] =
            "A subscription invoice already exists for this organization subscription and billing period, or this invoice number is already in use.",
        ["ux_subscription_payments_tenant_id_reference_number"] =
            "A subscription payment with this reference number has already been recorded for this organization."
    };

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
            && DuplicatePlatformBillingConstraints.TryGetValue(pg.ConstraintName, out string? message))
        {
            throw new ConflictException(message);
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
