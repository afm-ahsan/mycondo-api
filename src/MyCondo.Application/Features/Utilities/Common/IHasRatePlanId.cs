namespace MyCondo.Application.Features.Utilities.Common;

/// <summary>Implemented by rate-plan-scoped requests that carry a <c>RatePlanId</c> — their commercial
/// feature (Electricity vs. Gas) is resolved via <see cref="RatePlanFeatureResolver{TRequest}"/>
/// (ADR-033 Task 09A).</summary>
public interface IHasRatePlanId
{
    Guid RatePlanId { get; }
}
