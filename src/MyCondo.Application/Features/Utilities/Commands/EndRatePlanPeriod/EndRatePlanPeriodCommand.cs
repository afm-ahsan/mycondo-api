using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.EndRatePlanPeriod;

public sealed record EndRatePlanPeriodCommand(Guid RatePlanId, DateOnly EffectiveTo)
    : IRequest<RatePlanDto>, IHasRatePlanId, IRequiresResolvedFeature;
