namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record GeneratorFuelReceiptDto(
    Guid GeneratorFuelReceiptId,
    Guid GeneratorId,
    DateTimeOffset ReceivedAtUtc,
    decimal Quantity,
    decimal? Cost,
    string? Supplier,
    string? Remarks);
