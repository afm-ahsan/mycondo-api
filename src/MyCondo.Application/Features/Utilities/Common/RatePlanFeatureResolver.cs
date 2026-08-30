using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;

namespace MyCondo.Application.Features.Utilities.Common;

/// <summary>Resolves the commercial FeatureKey for any request carrying a <c>RatePlanId</c> (ADR-033 Task
/// 09A §17) — <see cref="RatePlan"/> carries its own <see cref="UtilityType"/> discriminator directly, with
/// no <c>MeterId</c> to join through. One dedicated implementation shared by the whole "RatePlanId" request
/// family via the generic constraint.</summary>
public sealed class RatePlanFeatureResolver<TRequest>(
    IRatePlanRepository ratePlans
) : IRequestFeatureResolver<TRequest>
    where TRequest : IHasRatePlanId
{
    public async Task<string> ResolveFeatureKeyAsync(TRequest request, Guid tenantId, CancellationToken cancellationToken)
    {
        RatePlanId id = new(request.RatePlanId);
        UtilityType utilityType = await ratePlans.GetUtilityTypeAsync(tenantId, id, cancellationToken)
            ?? throw new NotFoundException(nameof(RatePlan), request.RatePlanId);

        return UtilityFeatureKeyMapper.ToFeatureKey(utilityType);
    }
}
