namespace MyCondo.Domain.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Opens an explicit database transaction spanning multiple operations that must commit or roll
    /// back together — e.g. an invoice-sequence bump plus the invoice/line/ledger inserts it belongs
    /// to, or a payment's ledger posting plus its FIFO allocations against outstanding invoices. Most
    /// commands don't need this: a single <see cref="SaveChangesAsync"/> call is already one implicit
    /// transaction. Reach for this only when a raw-SQL statement (e.g. the sequence upsert) must
    /// commit atomically alongside a subsequent <see cref="SaveChangesAsync"/> call.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);

    Task RollbackAsync(CancellationToken ct = default);
}
