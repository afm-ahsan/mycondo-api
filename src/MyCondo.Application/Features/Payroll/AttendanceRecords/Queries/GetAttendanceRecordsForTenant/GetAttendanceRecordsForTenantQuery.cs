using Mediator;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForTenant;

/// <summary>
/// Tenant-wide attendance register. Pass <see cref="WorkDate"/> for "attendance on date X across all
/// staff"; pass <see cref="OnlyOpen"/> = true (typically with no <see cref="WorkDate"/>, or today's
/// date) for "who is currently clocked in". See IAttendanceRecordRepository.SearchForTenantAsync.
/// </summary>
public sealed record GetAttendanceRecordsForTenantQuery(
    DateOnly? WorkDate,
    Guid? StaffMemberId,
    bool? OnlyOpen,
    int Page,
    int PageSize
) : IRequest<PagedResult<AttendanceRegisterEntryDto>>;
