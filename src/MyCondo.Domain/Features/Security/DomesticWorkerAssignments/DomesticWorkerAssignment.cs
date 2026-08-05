using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Domain.Features.Security.DomesticWorkerAssignments;

/// <summary>
/// Links a <see cref="DomesticWorkerProfile"/> to a flat with a validity window and allowed
/// days/time-of-day — a worker may hold several of these (multi-flat assignment). "Expired" access is
/// derived by comparing <see cref="ValidToUtc"/> against the current time at check-in, not stored.
/// </summary>
public sealed class DomesticWorkerAssignment : AggregateRoot<DomesticWorkerAssignmentId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public DomesticWorkerProfileId DomesticWorkerProfileId { get; private set; }
    public FlatId FlatId { get; private set; }
    public bool ApprovedByResident { get; private set; }
    public DateTimeOffset ValidFromUtc { get; private set; }
    public DateTimeOffset? ValidToUtc { get; private set; }
    public DaysOfWeekFlags AllowedDays { get; private set; }
    public TimeOnly? AllowedStartTime { get; private set; }
    public TimeOnly? AllowedEndTime { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private DomesticWorkerAssignment() { }

    private DomesticWorkerAssignment(
        DomesticWorkerAssignmentId id,
        Guid tenantId,
        DomesticWorkerProfileId domesticWorkerProfileId,
        FlatId flatId,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc,
        DaysOfWeekFlags allowedDays,
        TimeOnly? allowedStartTime,
        TimeOnly? allowedEndTime,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        DomesticWorkerProfileId = domesticWorkerProfileId;
        FlatId = flatId;
        ApprovedByResident = false;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        AllowedDays = allowedDays;
        AllowedStartTime = allowedStartTime;
        AllowedEndTime = allowedEndTime;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static DomesticWorkerAssignment Create(
        Guid tenantId,
        DomesticWorkerProfileId domesticWorkerProfileId,
        FlatId flatId,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc,
        DaysOfWeekFlags allowedDays,
        TimeOnly? allowedStartTime,
        TimeOnly? allowedEndTime,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (validToUtc is not null && validToUtc < validFromUtc)
        {
            throw new ArgumentException("ValidToUtc cannot be before ValidFromUtc.", nameof(validToUtc));
        }

        return new DomesticWorkerAssignment(
            DomesticWorkerAssignmentId.New(), tenantId, domesticWorkerProfileId, flatId, validFromUtc, validToUtc,
            allowedDays == DaysOfWeekFlags.None ? DaysOfWeekFlags.All : allowedDays, allowedStartTime,
            allowedEndTime, nowUtc);
    }

    public void ApproveByResident()
    {
        ApprovedByResident = true;
        Version++;
    }

    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }

    /// <summary>True if <paramref name="localNow"/> (the tenant's local time — day-of-week/time-window
    /// checks are meaningless in UTC) falls within this assignment's validity window and allowed
    /// schedule.</summary>
    public bool IsCurrentlyValid(DateTimeOffset utcNow, DateTimeOffset localNow)
    {
        if (!IsActive || !ApprovedByResident)
        {
            return false;
        }

        if (utcNow < ValidFromUtc || (ValidToUtc is not null && utcNow > ValidToUtc))
        {
            return false;
        }

        DaysOfWeekFlags today = localNow.DayOfWeek switch
        {
            DayOfWeek.Monday => DaysOfWeekFlags.Monday,
            DayOfWeek.Tuesday => DaysOfWeekFlags.Tuesday,
            DayOfWeek.Wednesday => DaysOfWeekFlags.Wednesday,
            DayOfWeek.Thursday => DaysOfWeekFlags.Thursday,
            DayOfWeek.Friday => DaysOfWeekFlags.Friday,
            DayOfWeek.Saturday => DaysOfWeekFlags.Saturday,
            DayOfWeek.Sunday => DaysOfWeekFlags.Sunday,
            _ => DaysOfWeekFlags.None,
        };

        if (!AllowedDays.HasFlag(today))
        {
            return false;
        }

        TimeOnly localTime = TimeOnly.FromDateTime(localNow.DateTime);
        if (AllowedStartTime is not null && localTime < AllowedStartTime)
        {
            return false;
        }

        if (AllowedEndTime is not null && localTime > AllowedEndTime)
        {
            return false;
        }

        return true;
    }
}
