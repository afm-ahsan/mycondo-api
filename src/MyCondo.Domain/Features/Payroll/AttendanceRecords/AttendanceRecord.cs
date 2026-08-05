using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payroll.AttendanceRecords.Exceptions;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Domain.Features.Payroll.AttendanceRecords;

/// <summary>
/// One clock-in/clock-out pair for a <see cref="StaffMember"/>. A staff member may have several of
/// these per day (breaks, multiple gate passes) — "one open record per staff member" is still
/// enforced, same pattern as <c>AccessSession</c>. Late-arrival/early-departure/overtime are computed
/// from <see cref="ScheduledStartUtc"/>/<see cref="ScheduledEndUtc"/> vs. actual times rather than
/// stored, so they can never go stale relative to the source times.
/// </summary>
public sealed class AttendanceRecord : AggregateRoot<AttendanceRecordId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public StaffMemberId StaffMemberId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public DateTimeOffset? ScheduledStartUtc { get; private set; }
    public DateTimeOffset? ScheduledEndUtc { get; private set; }
    public DateTimeOffset CheckInUtc { get; private set; }
    public DateTimeOffset? CheckOutUtc { get; private set; }
    public string? WorkLocation { get; private set; }
    public AttendanceSource Source { get; private set; }
    public bool CorrectionRequested { get; private set; }
    public string? CorrectionReason { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsLateArrival => ScheduledStartUtc is not null && CheckInUtc > ScheduledStartUtc;

    public bool IsEarlyDeparture => ScheduledEndUtc is not null && CheckOutUtc is not null && CheckOutUtc < ScheduledEndUtc;

    public int OvertimeMinutes =>
        ScheduledEndUtc is not null && CheckOutUtc is not null && CheckOutUtc > ScheduledEndUtc
            ? (int)(CheckOutUtc.Value - ScheduledEndUtc.Value).TotalMinutes
            : 0;

    private AttendanceRecord() { }

    private AttendanceRecord(
        AttendanceRecordId id,
        Guid tenantId,
        StaffMemberId staffMemberId,
        DateOnly workDate,
        DateTimeOffset? scheduledStartUtc,
        DateTimeOffset? scheduledEndUtc,
        DateTimeOffset checkInUtc,
        string? workLocation,
        AttendanceSource source) : base(id)
    {
        TenantId = tenantId;
        StaffMemberId = staffMemberId;
        WorkDate = workDate;
        ScheduledStartUtc = scheduledStartUtc;
        ScheduledEndUtc = scheduledEndUtc;
        CheckInUtc = checkInUtc;
        WorkLocation = workLocation;
        Source = source;
        Version = 1;
        CreatedAtUtc = checkInUtc;
    }

    public static AttendanceRecord ClockIn(
        Guid tenantId,
        StaffMemberId staffMemberId,
        DateOnly workDate,
        DateTimeOffset? scheduledStartUtc,
        DateTimeOffset? scheduledEndUtc,
        DateTimeOffset checkInUtc,
        string? workLocation,
        AttendanceSource source)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new AttendanceRecord(
            AttendanceRecordId.New(), tenantId, staffMemberId, workDate, scheduledStartUtc, scheduledEndUtc,
            checkInUtc, workLocation?.Trim(), source);
    }

    public void ClockOut(DateTimeOffset checkOutUtc)
    {
        if (CheckOutUtc is not null)
        {
            throw new AttendanceRecordAlreadyClosedException(Id);
        }

        CheckOutUtc = checkOutUtc;
        Version++;
    }

    public void RequestCorrection(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        CorrectionRequested = true;
        CorrectionReason = reason.Trim();
        Version++;
    }

    public void ApproveCorrection(Guid approvedBy, DateTimeOffset nowUtc)
    {
        CorrectionRequested = false;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = nowUtc;
        Version++;
    }
}
