using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Utilities.RatePlans.Exceptions;

public sealed class RatePlanAlreadyEndedException(RatePlanId ratePlanId)
    : DomainException($"Rate plan {ratePlanId} already has an EffectiveTo date set.")
{
    public RatePlanId RatePlanId { get; } = ratePlanId;
}
