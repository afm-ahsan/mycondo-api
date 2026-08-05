namespace MyCondo.Application.Common;

/// <summary>
/// Converts UTC to the tenant's local time for day-of-week/time-window schedule checks (domestic
/// worker and service provider assignments) — per the register digitization spec §3.5, UTC is stored
/// throughout, and Asia/Dhaka is used only at these presentation/business-rule-evaluation boundaries.
/// Bangladesh has a single time zone with no DST, so a single hard-coded IANA id is sufficient; a
/// per-tenant timezone setting would be a follow-up if this platform ever serves tenants elsewhere.
/// </summary>
public static class DhakaTimeZone
{
    private static readonly TimeZoneInfo Dhaka = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");

    public static DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Dhaka);
}
