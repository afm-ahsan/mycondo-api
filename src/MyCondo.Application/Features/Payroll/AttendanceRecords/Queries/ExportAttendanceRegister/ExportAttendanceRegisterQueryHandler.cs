using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.ExportAttendanceRegister;

/// <summary>Reuses <see cref="GetAttendanceRecordsForTenantQuery"/> for the actual register data
/// (tenant scoping, filtering) rather than duplicating that logic. The on-screen register is
/// paginated, but export must return the FULL matching result set — so this handler pages through
/// <see cref="GetAttendanceRecordsForTenantQuery"/> internally at <see cref="MaxPageSize"/> (the
/// maximum <c>GetAttendanceRecordsForTenantQueryValidator</c> allows per call) until every matching
/// row has been retrieved, then maps the concatenated entries to one <see cref="ReportExportDocument"/>.
/// This never truncates: the loop keeps requesting pages until the accumulated row count reaches the
/// server-reported <see cref="PagedResult{T}.Total"/> (or a page comes back empty), so an arbitrarily
/// large register export is complete at the cost of additional round-trips rather than a capped
/// export. Unlike the Finance ledger reports, attendance rows carry no running balance, so
/// concatenation alone is sufficient — no re-derivation is needed once all pages are collected.
/// </summary>
public sealed class ExportAttendanceRegisterQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportAttendanceRegisterQuery, ReportExportResult>
{
    private const int MaxPageSize = 100;

    public async ValueTask<ReportExportResult> Handle(ExportAttendanceRegisterQuery query, CancellationToken cancellationToken)
    {
        List<AttendanceRegisterEntryDto> allEntries = [];
        PagedResult<AttendanceRegisterEntryDto> lastPage;
        int page = 1;
        do
        {
            lastPage = await sender.Send(
                new GetAttendanceRecordsForTenantQuery(
                    query.WorkDate, query.StaffMemberId, query.OnlyOpen, page, MaxPageSize),
                cancellationToken);
            allEntries.AddRange(lastPage.Items);
            page++;
        }
        while (lastPage.Items.Count > 0 && allEntries.Count < lastPage.Total);

        ReportExportDocument document = AttendanceRegisterExportMapper.ToExportDocument(
            allEntries, query.WorkDate, query.StaffMemberId, query.OnlyOpen);

        string fileName = $"attendance-register-{BuildDateSegment(query.WorkDate)}";
        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }

    private static string BuildDateSegment(DateOnly? workDate) =>
        (workDate ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
