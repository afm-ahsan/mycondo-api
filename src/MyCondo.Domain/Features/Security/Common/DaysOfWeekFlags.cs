namespace MyCondo.Domain.Features.Security.Common;

/// <summary>
/// Bitmask of allowed weekdays for a recurring access assignment (domestic worker, service provider).
/// Shared by both features since "allowed days/time windows" is identical logic in each.
/// </summary>
[Flags]
public enum DaysOfWeekFlags
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
}
