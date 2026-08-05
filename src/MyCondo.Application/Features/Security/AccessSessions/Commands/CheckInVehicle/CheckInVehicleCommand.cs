using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInVehicle;

public sealed record CheckInVehicleCommand(
    Guid VehicleId,
    Guid? HostFlatId,
    Guid EntryGateId,
    string? Remarks,
    string? OverrideReason
) : IRequest<AccessSessionDto>;
