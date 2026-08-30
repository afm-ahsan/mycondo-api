using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Property.Gates.Commands.ActivateGate;

public sealed record ActivateGateCommand(Guid GateId) : IRequest, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.gates";
}
