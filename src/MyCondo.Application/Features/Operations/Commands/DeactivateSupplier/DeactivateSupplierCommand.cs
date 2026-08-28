using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.DeactivateSupplier;

public sealed record DeactivateSupplierCommand(Guid GasCylinderSupplierId) : IRequest<GasCylinderSupplierDto>, IRequiresFeature
{
    public string FeatureKey => "operations.gas_cylinders";
}
