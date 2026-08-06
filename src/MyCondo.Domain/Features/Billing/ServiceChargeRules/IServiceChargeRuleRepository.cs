using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Billing.ServiceChargeRules;

public interface IServiceChargeRuleRepository
{
    Task<ServiceChargeRule?> GetByIdAsync(ServiceChargeRuleId id, CancellationToken cancellationToken);

    Task<PagedResult<ServiceChargeRule>> SearchAsync(
        Guid tenantId, BuildingId buildingId, string? category, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Rules that cover the given period in full — selection is by effective date, not just
    /// <see cref="ServiceChargeRule.IsActive"/>. See <see cref="ServiceChargeRule.AppliesToPeriod"/>.</summary>
    Task<IReadOnlyList<ServiceChargeRule>> GetApplicableRulesAsync(
        Guid tenantId, BuildingId buildingId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken);

    /// <summary>Application-layer pre-check ahead of the authoritative DB EXCLUDE constraint — gives a
    /// friendly 409 instead of a raw constraint-violation error for the common case.</summary>
    Task<bool> HasOverlappingRuleAsync(
        Guid tenantId,
        BuildingId buildingId,
        string category,
        FlatType? unitTypeFilter,
        BillingFrequency frequency,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        CancellationToken cancellationToken);

    void Add(ServiceChargeRule rule);
}
