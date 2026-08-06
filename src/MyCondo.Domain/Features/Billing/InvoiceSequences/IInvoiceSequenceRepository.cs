using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Billing.InvoiceSequences;

/// <summary>
/// Backs <c>billing.invoice_sequences</c> — a plain per-tenant/building/year counter, not a DDD
/// aggregate (no strongly-typed ID, no business behavior beyond "give me the next number"). Not
/// modeled as an entity for that reason; the repository is the entire abstraction.
/// </summary>
public interface IInvoiceSequenceRepository
{
    /// <summary>Atomically increments and returns the next sequence value for
    /// (tenantId, buildingId, year) via a single upsert+RETURNING statement — see
    /// <c>InvoiceSequenceRepository</c> for the exact SQL. Must be called inside the same
    /// <see cref="MyCondo.Domain.Abstractions.IUnitOfWork"/> transaction as the invoice insert it
    /// belongs to.</summary>
    Task<int> GetNextValueAsync(Guid tenantId, BuildingId buildingId, int year, CancellationToken cancellationToken);
}
