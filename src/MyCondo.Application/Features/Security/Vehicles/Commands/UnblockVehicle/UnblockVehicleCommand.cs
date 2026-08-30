using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.UnblockVehicle;

public sealed record UnblockVehicleCommand(Guid VehicleId) : IRequest, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.vehicles";
}
