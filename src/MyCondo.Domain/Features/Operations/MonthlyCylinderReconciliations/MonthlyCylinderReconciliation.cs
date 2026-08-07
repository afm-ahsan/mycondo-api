using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;

/// <summary>
/// A snapshot of a cylinder type's stock movement for one calendar month, computed and frozen at
/// creation time — immutable afterward, like <c>GeneratorServiceRecord</c>. <see cref="ClosingStock"/>
/// and <see cref="VarianceQuantity"/> are computed by the command handler from the actual
/// <c>CylinderStockMovements.CylinderStockMovement</c> ledger for the period and passed in already
/// resolved, not recomputed here — this entity records the reconciliation event, it doesn't own the
/// aggregation logic (that lives in the application layer alongside the report queries that use the
/// same ledger).
/// </summary>
public sealed class MonthlyCylinderReconciliation : Entity<MonthlyCylinderReconciliationId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string CylinderType { get; private set; }
    public DateOnly PeriodMonth { get; private set; }
    public int OpeningStock { get; private set; }
    public int TotalReceived { get; private set; }
    public int TotalIssued { get; private set; }
    public int TotalEmptyReturned { get; private set; }
    public int ClosingStock { get; private set; }
    public int VarianceQuantity { get; private set; }
    public string? Remarks { get; private set; }
    public Guid? ReconciledBy { get; private set; }
    public DateTimeOffset ReconciledAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private MonthlyCylinderReconciliation()
    {
        CylinderType = null!;
    }

    private MonthlyCylinderReconciliation(
        MonthlyCylinderReconciliationId id, Guid tenantId, string cylinderType, DateOnly periodMonth, int openingStock,
        int totalReceived, int totalIssued, int totalEmptyReturned, int closingStock, int varianceQuantity,
        string? remarks, Guid? reconciledBy, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        CylinderType = cylinderType;
        PeriodMonth = periodMonth;
        OpeningStock = openingStock;
        TotalReceived = totalReceived;
        TotalIssued = totalIssued;
        TotalEmptyReturned = totalEmptyReturned;
        ClosingStock = closingStock;
        VarianceQuantity = varianceQuantity;
        Remarks = remarks;
        ReconciledBy = reconciledBy;
        ReconciledAtUtc = nowUtc;
        CreatedAtUtc = nowUtc;
    }

    public static MonthlyCylinderReconciliation Create(
        Guid tenantId, string cylinderType, DateOnly periodMonth, int openingStock, int totalReceived, int totalIssued,
        int totalEmptyReturned, int actualClosingStock, string? remarks, Guid? reconciledBy, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cylinderType);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        int expectedClosingStock = openingStock + totalReceived - totalIssued - totalEmptyReturned;
        int variance = actualClosingStock - expectedClosingStock;

        return new MonthlyCylinderReconciliation(
            MonthlyCylinderReconciliationId.New(), tenantId, cylinderType.Trim(),
            new DateOnly(periodMonth.Year, periodMonth.Month, 1), openingStock, totalReceived, totalIssued,
            totalEmptyReturned, actualClosingStock, variance, remarks?.Trim(), reconciledBy, nowUtc);
    }
}
