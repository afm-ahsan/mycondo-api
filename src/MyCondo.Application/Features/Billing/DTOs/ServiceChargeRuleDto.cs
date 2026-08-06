namespace MyCondo.Application.Features.Billing.DTOs;

public sealed record ServiceChargeRuleDto(
    Guid ServiceChargeRuleId,
    Guid BuildingId,
    string Category,
    string Name,
    string CalculationMethod,
    decimal Rate,
    string? UnitTypeFilter,
    string Frequency,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive);
