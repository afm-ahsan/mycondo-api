using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.ReactivateFacility;

public sealed record ReactivateFacilityCommand(Guid FacilityId) : IRequest<FacilityDto>;
