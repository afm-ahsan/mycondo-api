using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationSecurityView;

public sealed record GetOccupancyRegistrationSecurityViewQuery(
    Guid OccupancyRegistrationId
) : IRequest<OccupancyRegistrationSecurityViewDto>;
