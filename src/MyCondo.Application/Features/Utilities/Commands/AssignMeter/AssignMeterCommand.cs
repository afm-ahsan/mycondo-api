using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.AssignMeter;

public sealed record AssignMeterCommand(Guid MeterId, Guid FlatId)
    : IRequest<MeterAssignmentDto>, IHasMeterId, IRequiresResolvedFeature, ILifecycleWriteOperation;
