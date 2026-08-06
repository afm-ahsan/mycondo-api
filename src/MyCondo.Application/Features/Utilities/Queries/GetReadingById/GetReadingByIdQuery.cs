using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadingById;

public sealed record GetReadingByIdQuery(Guid ReadingId) : IRequest<ReadingDto>;
