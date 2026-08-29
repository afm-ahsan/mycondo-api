using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.ReactivateSupplier;

public sealed record ReactivateSupplierCommand(Guid GasCylinderSupplierId) : IRequest<GasCylinderSupplierDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "operations.gas_cylinders";
}
