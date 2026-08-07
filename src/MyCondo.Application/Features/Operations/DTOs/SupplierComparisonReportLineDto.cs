namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record SupplierComparisonReportLineDto(
    Guid SupplierId,
    string SupplierName,
    int PurchaseCount,
    int TotalQuantity,
    decimal TotalAmount,
    decimal AverageUnitPricePerKg);
