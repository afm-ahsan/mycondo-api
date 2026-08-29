using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.MarkMeterFaulty;

public sealed record MarkMeterFaultyCommand(Guid MeterId, string Reason)
    : IRequest<MeterDto>, IHasMeterId, IRequiresResolvedFeature, ILifecycleWriteOperation;
