using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.RecordCylinderPurchase;

public sealed record RecordCylinderPurchaseCommand(
    Guid SupplierId,
    string InvoiceNumber,
    DateOnly PurchaseDate,
    string CylinderType,
    int Quantity,
    decimal CylinderWeightKg,
    decimal RatePerCylinder,
    decimal DeliveryOrOtherCost,
    string? Remarks
) : IRequest<CylinderPurchaseDto>;
