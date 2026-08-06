namespace MyCondo.Application.Features.Utilities.DTOs;

public sealed record RatePlanDto(
    Guid RatePlanId,
    Guid BuildingId,
    string UtilityType,
    string Name,
    string Structure,
    decimal? FixedAmount,
    decimal FixedServiceCharge,
    decimal TaxPercentage,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    IReadOnlyList<RateSlabDto> Slabs);
