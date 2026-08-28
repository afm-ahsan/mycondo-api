using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.RequestAttendanceCorrection;

public sealed record RequestAttendanceCorrectionCommand(Guid AttendanceRecordId, string Reason) : IRequest, IRequiresFeature
{
    public string FeatureKey => "security.staff_attendance";
}
