namespace MyCondo.Application.Features.Leasing;

/// <summary>
/// The allowed values for <c>HouseholdMember.RelationshipToPrimary</c> — enforced here at the validator
/// level rather than as a database enum, since existing Tenant Registration household data already has
/// arbitrary free-text relationship values and migrating it is out of scope; new/updated members are
/// constrained to this list going forward.
/// </summary>
public static class HouseholdRelationships
{
    public static readonly IReadOnlySet<string> Allowed =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Father", "Mother", "Spouse", "Child" };
}
