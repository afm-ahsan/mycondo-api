using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Mappings;

internal static class AttendanceRecordMappings
{
    public static AttendanceRecordDto ToDto(this AttendanceRecord record) => new(
        record.Id.Value, record.StaffMemberId.Value, record.WorkDate, record.ScheduledStartUtc,
        record.ScheduledEndUtc, record.CheckInUtc, record.CheckOutUtc, record.WorkLocation,
        record.Source.ToString(), record.CorrectionRequested, record.CorrectionReason, record.ApprovedBy,
        record.ApprovedAtUtc, record.IsLateArrival, record.IsEarlyDeparture, record.OvertimeMinutes);
}
