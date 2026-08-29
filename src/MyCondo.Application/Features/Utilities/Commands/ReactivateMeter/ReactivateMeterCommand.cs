using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.ReactivateMeter;

public sealed record ReactivateMeterCommand(Guid MeterId)
    : IRequest<MeterDto>, IHasMeterId, IRequiresResolvedFeature, ILifecycleWriteOperation;
