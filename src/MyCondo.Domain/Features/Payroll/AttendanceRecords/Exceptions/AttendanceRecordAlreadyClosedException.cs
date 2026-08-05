using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Payroll.AttendanceRecords.Exceptions;

public sealed class AttendanceRecordAlreadyClosedException(AttendanceRecordId attendanceRecordId)
    : DomainException($"Attendance record {attendanceRecordId} is already clocked out.")
{
    public AttendanceRecordId AttendanceRecordId { get; } = attendanceRecordId;
}
