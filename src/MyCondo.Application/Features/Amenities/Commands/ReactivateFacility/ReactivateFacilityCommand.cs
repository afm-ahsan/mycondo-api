using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.Common;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.ReactivateFacility;

public sealed record ReactivateFacilityCommand(Guid FacilityId)
    : IRequest<FacilityDto>, IHasFacilityId, IRequiresResolvedFeature, ILifecycleWriteOperation;
