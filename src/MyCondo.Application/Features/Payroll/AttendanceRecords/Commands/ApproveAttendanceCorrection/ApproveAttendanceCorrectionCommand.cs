using Mediator;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ApproveAttendanceCorrection;

public sealed record ApproveAttendanceCorrectionCommand(Guid AttendanceRecordId) : IRequest;
