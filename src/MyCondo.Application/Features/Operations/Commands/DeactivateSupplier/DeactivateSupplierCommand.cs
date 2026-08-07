using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.DeactivateSupplier;

public sealed record DeactivateSupplierCommand(Guid GasCylinderSupplierId) : IRequest<GasCylinderSupplierDto>;
