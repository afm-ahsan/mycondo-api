using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForTenant;

public sealed class GetAttendanceRecordsForTenantQueryHandler(
    IAttendanceRecordRepository attendanceRecords,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAttendanceRecordsForTenantQuery, PagedResult<AttendanceRegisterEntryDto>>
{
    public async ValueTask<PagedResult<AttendanceRegisterEntryDto>> Handle(
        GetAttendanceRecordsForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<AttendanceRegisterEntry> result = await attendanceRecords.SearchForTenantAsync(
            tenantId,
            query.WorkDate,
            query.StaffMemberId is Guid id ? new StaffMemberId(id) : null,
            query.OnlyOpen,
            query.Page,
            query.PageSize,
            cancellationToken);

        List<AttendanceRegisterEntryDto> items = result.Items.Select(e => e.ToDto()).ToList();

        return new PagedResult<AttendanceRegisterEntryDto>(items, result.Page, result.PageSize, result.Total);
    }
}
