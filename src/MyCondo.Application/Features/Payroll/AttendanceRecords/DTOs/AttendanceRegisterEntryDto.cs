namespace MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;

/// <summary>
/// One row of the tenant-wide attendance register/currently-present view — see
/// MyCondo.Domain.Features.Payroll.AttendanceRecords.AttendanceRegisterEntry for the rationale.
/// </summary>
public sealed record AttendanceRegisterEntryDto(
    Guid AttendanceRecordId,
    Guid StaffMemberId,
    string StaffMemberFullName,
    string StaffMemberRole,
    DateOnly WorkDate,
    DateTimeOffset? ScheduledStartUtc,
    DateTimeOffset? ScheduledEndUtc,
    DateTimeOffset CheckInUtc,
    DateTimeOffset? CheckOutUtc,
    string? WorkLocation,
    string Source,
    bool CorrectionRequested,
    string? CorrectionReason,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAtUtc,
    bool IsLateArrival,
    bool IsEarlyDeparture,
    int OvertimeMinutes);
