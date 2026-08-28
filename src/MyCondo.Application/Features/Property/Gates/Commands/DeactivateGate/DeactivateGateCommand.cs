using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Property.Gates.Commands.DeactivateGate;

public sealed record DeactivateGateCommand(Guid GateId) : IRequest, IRequiresFeature
{
    public string FeatureKey => "security.gates";
}
