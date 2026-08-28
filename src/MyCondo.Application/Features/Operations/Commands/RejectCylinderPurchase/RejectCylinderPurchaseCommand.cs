using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.RejectCylinderPurchase;

public sealed record RejectCylinderPurchaseCommand(Guid CylinderPurchaseId, string Reason) : IRequest<CylinderPurchaseDto>, IRequiresFeature
{
    public string FeatureKey => "operations.gas_cylinders";
}
