using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.ReviewReading;

public sealed record ReviewReadingCommand(Guid ReadingId)
    : IRequest<ReadingDto>, IHasReadingId, IRequiresResolvedFeature, ILifecycleWriteOperation;
