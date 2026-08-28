using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockOut;

public sealed record ClockOutCommand(Guid AttendanceRecordId) : IRequest<AttendanceRecordDto>, IRequiresFeature
{
    public string FeatureKey => "security.staff_attendance";
}
