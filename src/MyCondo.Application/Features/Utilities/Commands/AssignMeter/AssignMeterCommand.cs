using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.AssignMeter;

public sealed record AssignMeterCommand(Guid MeterId, Guid FlatId) : IRequest<MeterAssignmentDto>;
