using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.BlockVehicle;

public sealed record BlockVehicleCommand(Guid VehicleId, string Reason) : IRequest, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.vehicles";
}
