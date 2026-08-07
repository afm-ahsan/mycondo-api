using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(
    Guid GasCylinderSupplierId,
    string Name,
    string? ContactPhone,
    string? ContactEmail,
    string? Address
) : IRequest<GasCylinderSupplierDto>;
