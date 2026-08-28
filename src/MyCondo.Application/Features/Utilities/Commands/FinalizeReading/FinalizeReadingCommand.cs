using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.FinalizeReading;

public sealed record FinalizeReadingCommand(Guid ReadingId)
    : IRequest<ReadingDto>, IHasReadingId, IRequiresResolvedFeature;
