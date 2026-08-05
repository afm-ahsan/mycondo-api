using Mediator;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.RequestAttendanceCorrection;

public sealed record RequestAttendanceCorrectionCommand(Guid AttendanceRecordId, string Reason) : IRequest;
