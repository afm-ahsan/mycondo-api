using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.DeactivateFacility;

public sealed record DeactivateFacilityCommand(Guid FacilityId) : IRequest<FacilityDto>;
