using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockIn;

public sealed record ClockInCommand(
    Guid StaffMemberId,
    DateOnly WorkDate,
    DateTimeOffset? ScheduledStartUtc,
    DateTimeOffset? ScheduledEndUtc,
    string? WorkLocation,
    string Source
) : IRequest<AttendanceRecordDto>, IRequiresFeature
{
    public string FeatureKey => "security.staff_attendance";
}
