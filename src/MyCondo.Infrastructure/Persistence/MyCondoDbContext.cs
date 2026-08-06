using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MyCondo.Domain.Abstractions;

namespace MyCondo.Infrastructure.Persistence;

public sealed class MyCondoDbContext(
    DbContextOptions<MyCondoDbContext> options
) : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema-per-module: each module's IEntityTypeConfiguration<T> sets ToTable(name, schema: "<module>").
        // No HasDefaultSchema() — every aggregate must declare its schema explicitly to avoid silent fallthrough.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyCondoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
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
