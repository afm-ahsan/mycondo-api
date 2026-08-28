using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForStaffMember;

public sealed record GetAttendanceRecordsForStaffMemberQuery(
    Guid StaffMemberId,
    int Page,
    int PageSize
) : IRequest<PagedResult<AttendanceRecordDto>>, IRequiresFeature
{
    public string FeatureKey => "security.staff_attendance";
}
