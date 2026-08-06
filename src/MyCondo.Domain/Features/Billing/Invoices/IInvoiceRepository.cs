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

    void Add(Invoice invoice);

    void AddLines(IEnumerable<InvoiceLine> lines);
}
