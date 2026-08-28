namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

/// <summary>
/// ADR-033 §22: every feature seeded today is <see cref="Boolean"/>. <see cref="Numeric"/> is this
/// enum's name for what the ADR calls "Integer" — renamed only to satisfy CA1720 (identifier must not
/// contain a type name); the stored column value and semantics are otherwise identical. Reserved for a
/// future <c>limit.*</c>-style quota feature (e.g. a building/user count cap) — no such feature exists
/// yet, and no quota-enforcement engine is built by this reservation.
/// </summary>
public enum FeatureEntitlementType
{
    Boolean = 0,
    Numeric = 1
}
