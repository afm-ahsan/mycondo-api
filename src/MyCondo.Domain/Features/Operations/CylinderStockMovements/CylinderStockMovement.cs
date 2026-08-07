using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.CylinderPurchases;

namespace MyCondo.Domain.Features.Operations.CylinderStockMovements;

/// <summary>
/// Append-only stock ledger entry for a cylinder type — mirrors the <c>payments.ledger_entries</c>
/// philosophy (no deletes/edits, corrections are new entries) rather than a mutable stock counter.
/// <see cref="Quantity"/> is the already-signed delta applied to on-hand stock (positive = stock
/// increases, negative = stock decreases); current stock for a cylinder type is always the sum of
/// every movement's <see cref="Quantity"/>, computed at query time, never stored. "Controlled
/// adjustment only with reason and authorization" (register-digitization spec §5.14) is enforced by
/// <see cref="Adjust"/> requiring a non-empty reason and the caller holding <c>gascylinder.approve</c>
/// (checked in the command handler, not here — permission checks aren't a domain concern).
/// </summary>
public sealed class CylinderStockMovement : Entity<CylinderStockMovementId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string CylinderType { get; private set; }
    public CylinderStockMovementType MovementType { get; private set; }
    public int Quantity { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public Guid? RecordedBy { get; private set; }
    public CylinderPurchaseId? CylinderPurchaseId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private CylinderStockMovement()
    {
        CylinderType = null!;
    }

    private CylinderStockMovement(
        CylinderStockMovementId id, Guid tenantId, string cylinderType, CylinderStockMovementType movementType,
        int quantity, DateTimeOffset occurredAtUtc, string? reason, Guid? recordedBy,
        CylinderPurchaseId? cylinderPurchaseId, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        CylinderType = cylinderType;
        MovementType = movementType;
        Quantity = quantity;
        OccurredAtUtc = occurredAtUtc;
        Reason = reason;
        RecordedBy = recordedBy;
        CylinderPurchaseId = cylinderPurchaseId;
        CreatedAtUtc = nowUtc;
    }

    public static CylinderStockMovement Receive(
        Guid tenantId, string cylinderType, int quantity, DateTimeOffset occurredAtUtc, Guid? recordedBy,
        CylinderPurchaseId? cylinderPurchaseId, DateTimeOffset nowUtc)
    {
        ValidateTenantId(tenantId);
        ValidatePositiveQuantity(quantity);
        return new CylinderStockMovement(
            CylinderStockMovementId.New(), tenantId, ValidateCylinderType(cylinderType), CylinderStockMovementType.Receipt,
            quantity, occurredAtUtc, null, recordedBy, cylinderPurchaseId, nowUtc);
    }

    public static CylinderStockMovement Issue(
        Guid tenantId, string cylinderType, int quantity, DateTimeOffset occurredAtUtc, Guid? recordedBy, DateTimeOffset nowUtc)
    {
        ValidateTenantId(tenantId);
        ValidatePositiveQuantity(quantity);
        return new CylinderStockMovement(
            CylinderStockMovementId.New(), tenantId, ValidateCylinderType(cylinderType), CylinderStockMovementType.Issue,
            -quantity, occurredAtUtc, null, recordedBy, null, nowUtc);
    }

    public static CylinderStockMovement ReturnEmpty(
        Guid tenantId, string cylinderType, int quantity, DateTimeOffset occurredAtUtc, Guid? recordedBy, DateTimeOffset nowUtc)
    {
        ValidateTenantId(tenantId);
        ValidatePositiveQuantity(quantity);
        return new CylinderStockMovement(
            CylinderStockMovementId.New(), tenantId, ValidateCylinderType(cylinderType), CylinderStockMovementType.EmptyReturn,
            -quantity, occurredAtUtc, null, recordedBy, null, nowUtc);
    }

    public static CylinderStockMovement Adjust(
        Guid tenantId, string cylinderType, int signedQuantity, string reason, DateTimeOffset occurredAtUtc, Guid? recordedBy,
        DateTimeOffset nowUtc)
    {
        ValidateTenantId(tenantId);
        if (signedQuantity == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(signedQuantity), "Adjustment quantity cannot be zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new CylinderStockMovement(
            CylinderStockMovementId.New(), tenantId, ValidateCylinderType(cylinderType), CylinderStockMovementType.Adjustment,
            signedQuantity, occurredAtUtc, reason.Trim(), recordedBy, null, nowUtc);
    }

    private static void ValidateTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }
    }

    private static void ValidatePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }
    }

    private static string ValidateCylinderType(string cylinderType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cylinderType);
        return cylinderType.Trim();
    }
}
