using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolSessionById;

public sealed record GetPoolSessionByIdQuery(Guid PoolSessionId) : IRequest<PoolSessionDto>;
