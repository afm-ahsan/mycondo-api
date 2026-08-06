using Mediator;
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
) : IRequest<RatePlanDto>;
