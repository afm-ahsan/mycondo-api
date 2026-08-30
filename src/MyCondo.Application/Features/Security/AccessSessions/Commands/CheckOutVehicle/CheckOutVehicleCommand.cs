using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutVehicle;

public sealed record CheckOutVehicleCommand(Guid AccessSessionId, Guid ExitGateId) : IRequest<AccessSessionDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.vehicles";
}
