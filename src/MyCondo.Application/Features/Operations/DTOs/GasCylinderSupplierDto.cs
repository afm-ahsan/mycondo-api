namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record GasCylinderSupplierDto(
    Guid GasCylinderSupplierId,
    string Name,
    string? ContactPhone,
    string? ContactEmail,
    string? Address,
    bool IsActive);
