using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>One flat's total outstanding receivable across every open (Issued/PartiallyPaid/
/// PartiallyWaived) invoice — the Outstanding Dues report's per-flat grouping. Unlike
/// <see cref="AgeingReceivableLine"/> (which deliberately omits FlatId since Receivable Ageing only
/// buckets by age), this line exists specifically to group/sum by flat.</summary>
public sealed record FlatReceivableLine(FlatId FlatId, decimal Balance, int OpenInvoiceCount);

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

    /// <summary>Tenant-wide (optionally building-scoped) financial aggregate for reporting. TotalBilled
    /// is computed over invoices with <c>InvoiceDate</c> in [fromDate, toDate], excluding Void. All other
    /// fields are current-snapshot values as of <paramref name="asOfDate"/> — see
    /// <see cref="InvoiceFinancialAggregate"/>.</summary>
    Task<InvoiceFinancialAggregate> GetFinancialAggregateAsync(
        Guid tenantId, BuildingId? buildingId, DateOnly fromDate, DateOnly toDate, DateOnly asOfDate,
        CancellationToken cancellationToken);

    /// <summary>All open (Issued/PartiallyPaid) invoices for ageing, as a minimal (DueDate, Balance)
    /// projection — tenant/building/status filtering is SQL-pushed; bucketing by AsOfDate happens in
    /// the caller, since the open-receivables population is inherently bounded (not the full invoice
    /// history) and DateOnly day-difference arithmetic is not reliably translatable across EF providers.</summary>
    Task<IReadOnlyList<AgeingReceivableLine>> GetOpenReceivablesAsync(
        Guid tenantId, BuildingId? buildingId, CancellationToken cancellationToken);

    /// <summary>Same open-invoice population as <see cref="GetOpenReceivablesAsync"/>, pre-aggregated
    /// per Flat server-side — the Outstanding Dues report's data source (simple per-flat total, not
    /// age-bucketed; see <see cref="FlatReceivableLine"/>).</summary>
    Task<IReadOnlyList<FlatReceivableLine>> GetOpenReceivablesByFlatAsync(
        Guid tenantId, BuildingId? buildingId, CancellationToken cancellationToken);

    void Add(Invoice invoice);

    void AddLines(IEnumerable<InvoiceLine> lines);
}
