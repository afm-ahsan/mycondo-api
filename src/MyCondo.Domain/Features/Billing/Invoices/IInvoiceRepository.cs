using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Billing.Invoices;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken cancellationToken);

    Task<PagedResult<Invoice>> SearchAsync(
        Guid tenantId,
        BuildingId? buildingId,
        FlatId? flatId,
        InvoiceStatus? status,
        InvoiceSource? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> ExistsForFlatAndPeriodAsync(
        Guid tenantId, FlatId flatId, DateOnly periodStart, DateOnly periodEnd, InvoiceSource source,
        CancellationToken cancellationToken);

    /// <summary>FIFO order: due date, then invoice date, then invoice number as the final
    /// deterministic tie-breaker. Rows are locked (<c>FOR UPDATE</c>) by the implementation — see
    /// <c>financial-engine.md</c> invariant 5.</summary>
    Task<IReadOnlyList<Invoice>> GetOutstandingForFlatForUpdateAsync(
        Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InvoiceLine>> GetLinesForInvoiceAsync(InvoiceId invoiceId, CancellationToken cancellationToken);

    /// <summary>Sum of <see cref="Invoice.Balance"/> across a flat's outstanding invoices — a
    /// read-only eligibility check (Slice G's pool overdue-balance gate), not a locking financial-engine
    /// read, so it deliberately does not use <see cref="GetOutstandingForFlatForUpdateAsync"/>'s
    /// <c>FOR UPDATE</c> query.</summary>
    Task<decimal> GetOutstandingBalanceForFlatAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    void Add(Invoice invoice);

    void AddLines(IEnumerable<InvoiceLine> lines);
}
