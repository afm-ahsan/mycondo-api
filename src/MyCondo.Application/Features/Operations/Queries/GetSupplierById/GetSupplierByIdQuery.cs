using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetSupplierById;

public sealed record GetSupplierByIdQuery(Guid GasCylinderSupplierId) : IRequest<GasCylinderSupplierDto>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "operations.gas_cylinders";
}
