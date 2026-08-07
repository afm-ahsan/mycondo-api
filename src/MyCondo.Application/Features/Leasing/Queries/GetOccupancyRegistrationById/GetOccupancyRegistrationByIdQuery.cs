using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationById;

public sealed record GetOccupancyRegistrationByIdQuery(Guid OccupancyRegistrationId) : IRequest<OccupancyRegistrationDto>;
