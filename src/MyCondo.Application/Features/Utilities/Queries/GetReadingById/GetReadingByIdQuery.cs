using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadingById;

public sealed record GetReadingByIdQuery(Guid ReadingId)
    : IRequest<ReadingDto>, IHasReadingId, IRequiresResolvedFeature;
