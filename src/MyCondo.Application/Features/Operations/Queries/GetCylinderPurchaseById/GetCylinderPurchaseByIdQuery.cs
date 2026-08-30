using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderPurchaseById;

public sealed record GetCylinderPurchaseByIdQuery(Guid CylinderPurchaseId) : IRequest<CylinderPurchaseDto>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "operations.gas_cylinders";
}
