using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.RejectOccupancyRegistrationByOwner;

public sealed record RejectOccupancyRegistrationByOwnerCommand(
    Guid OccupancyRegistrationId, string Reason
) : IRequest<OccupancyRegistrationDto>;
