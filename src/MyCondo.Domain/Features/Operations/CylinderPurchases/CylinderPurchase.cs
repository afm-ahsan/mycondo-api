using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.CylinderPurchases.Exceptions;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Domain.Features.Operations.CylinderPurchases;

/// <summary>
/// A single gas-cylinder purchase transaction (register-digitization spec §5.14). Computed fields
/// (<see cref="TotalKg"/>, <see cref="LineAmount"/>, <see cref="UnitPricePerKg"/>,
/// <see cref="GrandTotal"/>) are never stored — always derived from the authoritative stored fields, so
/// they can never drift out of sync and are never trusted from the client, matching the spec's
/// explicit server-calculation rule. Approval and payment are two independent tracks (mirrors
/// <c>Booking</c>'s ApprovalRequired/PaymentRequired split) — <see cref="MarkPaid"/> requires
/// <see cref="CylinderPurchaseApprovalStatus.Approved"/> first, since paying for an unapproved or
/// rejected purchase isn't a valid real-world sequence.
/// </summary>
public sealed class CylinderPurchase : AggregateRoot<CylinderPurchaseId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public GasCylinderSupplierId SupplierId { get; private set; }
    public string InvoiceNumber { get; private set; }
    public DateOnly PurchaseDate { get; private set; }
    public string CylinderType { get; private set; }
    public int Quantity { get; private set; }
    public decimal CylinderWeightKg { get; private set; }
    public decimal RatePerCylinder { get; private set; }
    public decimal DeliveryOrOtherCost { get; private set; }
    public string? Remarks { get; private set; }
    public CylinderPurchasePaymentStatus PaymentStatus { get; private set; }
    public CylinderPurchaseApprovalStatus ApprovalStatus { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? RejectedReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>Quantity × CylinderWeightKg.</summary>
    public decimal TotalKg => Quantity * CylinderWeightKg;

    /// <summary>Quantity × RatePerCylinder.</summary>
    public decimal LineAmount => Quantity * RatePerCylinder;

    /// <summary>RatePerCylinder ÷ CylinderWeightKg.</summary>
    public decimal UnitPricePerKg => CylinderWeightKg == 0 ? 0 : RatePerCylinder / CylinderWeightKg;

    /// <summary>LineAmount + DeliveryOrOtherCost.</summary>
    public decimal GrandTotal => LineAmount + DeliveryOrOtherCost;

    private CylinderPurchase()
    {
        InvoiceNumber = null!;
        CylinderType = null!;
    }

    private CylinderPurchase(
        CylinderPurchaseId id, Guid tenantId, GasCylinderSupplierId supplierId, string invoiceNumber,
        DateOnly purchaseDate, string cylinderType, int quantity, decimal cylinderWeightKg, decimal ratePerCylinder,
        decimal deliveryOrOtherCost, string? remarks, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        SupplierId = supplierId;
        InvoiceNumber = invoiceNumber;
        PurchaseDate = purchaseDate;
        CylinderType = cylinderType;
        Quantity = quantity;
        CylinderWeightKg = cylinderWeightKg;
        RatePerCylinder = ratePerCylinder;
        DeliveryOrOtherCost = deliveryOrOtherCost;
        Remarks = remarks;
        PaymentStatus = CylinderPurchasePaymentStatus.Unpaid;
        ApprovalStatus = CylinderPurchaseApprovalStatus.PendingApproval;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static CylinderPurchase Record(
        Guid tenantId, GasCylinderSupplierId supplierId, string invoiceNumber, DateOnly purchaseDate,
        string cylinderType, int quantity, decimal cylinderWeightKg, decimal ratePerCylinder,
        decimal deliveryOrOtherCost, string? remarks, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(cylinderType);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (cylinderWeightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cylinderWeightKg), "CylinderWeightKg must be positive.");
        }

        if (ratePerCylinder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePerCylinder), "RatePerCylinder cannot be negative.");
        }

        if (deliveryOrOtherCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryOrOtherCost), "DeliveryOrOtherCost cannot be negative.");
        }

        return new CylinderPurchase(
            CylinderPurchaseId.New(), tenantId, supplierId, invoiceNumber.Trim(), purchaseDate, cylinderType.Trim(),
            quantity, cylinderWeightKg, ratePerCylinder, deliveryOrOtherCost, remarks?.Trim(), nowUtc);
    }

    public void Approve(Guid? approvedBy, DateTimeOffset nowUtc)
    {
        if (ApprovalStatus != CylinderPurchaseApprovalStatus.PendingApproval)
        {
            throw new CylinderPurchaseInvalidTransitionException(Id, ApprovalStatus, "be approved");
        }

        ApprovalStatus = CylinderPurchaseApprovalStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = nowUtc;
        Version++;
    }

    public void Reject(string reason)
    {
        if (ApprovalStatus != CylinderPurchaseApprovalStatus.PendingApproval)
        {
            throw new CylinderPurchaseInvalidTransitionException(Id, ApprovalStatus, "be rejected");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        ApprovalStatus = CylinderPurchaseApprovalStatus.Rejected;
        RejectedReason = reason.Trim();
        Version++;
    }

    public void MarkPaid()
    {
        if (ApprovalStatus != CylinderPurchaseApprovalStatus.Approved)
        {
            throw new CylinderPurchaseInvalidTransitionException(Id, ApprovalStatus, "be marked paid (must be approved first)");
        }

        PaymentStatus = CylinderPurchasePaymentStatus.Paid;
        Version++;
    }
}
