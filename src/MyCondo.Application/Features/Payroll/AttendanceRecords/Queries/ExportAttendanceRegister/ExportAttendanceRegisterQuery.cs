using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.ExportAttendanceRegister;

/// <summary>No Page/PageSize here, unlike <see cref="GetAttendanceRecordsForTenant.GetAttendanceRecordsForTenantQuery"/>
/// — export always returns the FULL matching result set for the given filters (see
/// <see cref="ExportAttendanceRegisterQueryHandler"/>), never just the current on-screen page of the
/// register.</summary>
public sealed record ExportAttendanceRegisterQuery(
    DateOnly? WorkDate,
    Guid? StaffMemberId,
    bool? OnlyOpen,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
