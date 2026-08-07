using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.CreateBlackoutDate;

public sealed record CreateBlackoutDateCommand(
    Guid FacilityId,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Reason
) : IRequest<BlackoutDateDto>;
