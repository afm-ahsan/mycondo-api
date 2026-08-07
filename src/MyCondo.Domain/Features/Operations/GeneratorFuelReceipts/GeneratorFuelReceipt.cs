using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;

/// <summary>
/// A single fuel-received entry for a <see cref="Generator"/>, distinct from
/// <c>GeneratorSession.OpeningFuelLevel</c>/<c>ClosingFuelLevel</c> (per-session tank level) — this is
/// the "Fuel received" register line the spec's "fuel reconciliation must be possible" rule needs
/// (received vs. consumed, computed as a report over both this and session fuel-level deltas, not
/// stored here). Immutable once recorded, like a ledger entry — corrections are new entries, not edits.
/// </summary>
public sealed class GeneratorFuelReceipt : Entity<GeneratorFuelReceiptId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public GeneratorId GeneratorId { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal? Cost { get; private set; }
    public string? Supplier { get; private set; }
    public string? Remarks { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private GeneratorFuelReceipt() { }

    private GeneratorFuelReceipt(
        GeneratorFuelReceiptId id, Guid tenantId, GeneratorId generatorId, DateTimeOffset receivedAtUtc,
        decimal quantity, decimal? cost, string? supplier, string? remarks, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        GeneratorId = generatorId;
        ReceivedAtUtc = receivedAtUtc;
        Quantity = quantity;
        Cost = cost;
        Supplier = supplier;
        Remarks = remarks;
        CreatedAtUtc = nowUtc;
    }

    public static GeneratorFuelReceipt Record(
        Guid tenantId, GeneratorId generatorId, DateTimeOffset receivedAtUtc, decimal quantity, decimal? cost,
        string? supplier, string? remarks, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (cost is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Cost cannot be negative.");
        }

        return new GeneratorFuelReceipt(
            GeneratorFuelReceiptId.New(), tenantId, generatorId, receivedAtUtc, quantity, cost, supplier?.Trim(),
            remarks?.Trim(), nowUtc);
    }
}
