using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.ApproveCylinderPurchase;

public sealed record ApproveCylinderPurchaseCommand(Guid CylinderPurchaseId) : IRequest<CylinderPurchaseDto>, IRequiresFeature
{
    public string FeatureKey => "operations.gas_cylinders";
}
