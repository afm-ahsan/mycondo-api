namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record CylinderPurchaseDto(
    Guid CylinderPurchaseId,
    Guid SupplierId,
    string InvoiceNumber,
    DateOnly PurchaseDate,
    string CylinderType,
    int Quantity,
    decimal CylinderWeightKg,
    decimal RatePerCylinder,
    decimal DeliveryOrOtherCost,
    string? Remarks,
    string PaymentStatus,
    string ApprovalStatus,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAtUtc,
    string? RejectedReason,
    decimal TotalKg,
    decimal LineAmount,
    decimal UnitPricePerKg,
    decimal GrandTotal);
