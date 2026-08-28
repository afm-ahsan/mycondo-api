using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.DeactivateRatePlan;

public sealed record DeactivateRatePlanCommand(Guid RatePlanId)
    : IRequest<RatePlanDto>, IHasRatePlanId, IRequiresResolvedFeature;
