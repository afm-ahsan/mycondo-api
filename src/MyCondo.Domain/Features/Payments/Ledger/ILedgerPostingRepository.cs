namespace MyCondo.Domain.Features.Payments.Ledger;

public interface ILedgerPostingRepository
{
    void Add(LedgerPosting posting);

    /// <summary>Application-layer pre-check backing <c>IFinancialPostingService</c>'s idempotency guard
    /// — the database's partial unique index on (TenantId, ReferenceType, ReferenceId) is the actual
    /// guarantee (ADR-027); this just turns a would-be constraint violation into a clear 409 before any
    /// work is staged.</summary>
    Task<bool> ExistsAsync(Guid tenantId, string referenceType, Guid sourceId, CancellationToken cancellationToken);
}
