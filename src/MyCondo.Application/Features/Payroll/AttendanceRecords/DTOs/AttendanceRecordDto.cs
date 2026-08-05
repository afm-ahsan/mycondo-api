namespace MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;

public sealed record AttendanceRecordDto(
    Guid AttendanceRecordId,
    Guid StaffMemberId,
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
