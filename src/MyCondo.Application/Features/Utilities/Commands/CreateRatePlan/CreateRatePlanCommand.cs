using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.CreateRatePlan;

public sealed record CreateRatePlanCommand(
    Guid BuildingId,
    string UtilityType,
    string Name,
    string Structure,
    decimal? FixedAmount,
    decimal FixedServiceCharge,
    decimal TaxPercentage,
    DateOnly EffectiveFrom,
    IReadOnlyList<RateSlabInputDto> Slabs
) : IRequest<RatePlanDto>, IRequiresFeature
{
    // UtilityType is required here (unlike the nullable filter on GetRatePlansQuery), so the feature this
    // call actually uses is determinate from the request itself — no DB lookup needed.
    public string FeatureKey => UtilityType == "Gas" ? "utilities.gas" : "utilities.electricity";
}
