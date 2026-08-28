using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ApproveAttendanceCorrection;

public sealed record ApproveAttendanceCorrectionCommand(Guid AttendanceRecordId) : IRequest, IRequiresFeature
{
    public string FeatureKey => "security.staff_attendance";
}
