using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForStaffMember;

public sealed class GetAttendanceRecordsForStaffMemberQueryHandler(
    IAttendanceRecordRepository attendanceRecords,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAttendanceRecordsForStaffMemberQuery, PagedResult<AttendanceRecordDto>>
{
    public async ValueTask<PagedResult<AttendanceRecordDto>> Handle(GetAttendanceRecordsForStaffMemberQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<AttendanceRecord> result = await attendanceRecords.SearchForStaffMemberAsync(
            tenantId, new StaffMemberId(query.StaffMemberId), query.Page, query.PageSize, cancellationToken);

        List<AttendanceRecordDto> items = result.Items.Select(r => r.ToDto()).ToList();

        return new PagedResult<AttendanceRecordDto>(items, result.Page, result.PageSize, result.Total);
    }
}
