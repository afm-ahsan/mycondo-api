using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.ReactivateSupplier;

public sealed record ReactivateSupplierCommand(Guid GasCylinderSupplierId) : IRequest<GasCylinderSupplierDto>;
