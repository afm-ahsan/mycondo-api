using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.ExportAttendanceRegister;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForTenant;
using MyCondo.Domain.Common;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payroll.AttendanceRecords.Queries.ExportAttendanceRegister;

public class ExportAttendanceRegisterQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportAttendanceRegisterQueryHandler CreateHandler() => new(_sender, _exportService);

    private static AttendanceRegisterEntryDto Entry() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Jane Doe", "Security Guard", new DateOnly(2026, 8, 15),
        null, null, new DateTimeOffset(2026, 8, 15, 8, 0, 0, TimeSpan.Zero), null,
        "Main Gate", "Manual", false, null, null, null, false, false, 0);

    [Fact]
    public async Task Single_Page_Result_Fetches_Once_And_Exports_All_Entries()
    {
        PagedResult<AttendanceRegisterEntryDto> page1 = new([Entry(), Entry()], 1, 100, 2);
        _sender.Send(Arg.Any<GetAttendanceRecordsForTenantQuery>(), Arg.Any<CancellationToken>()).Returns(page1);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "attendance-register-2026-08-15.csv");
        _exportService
            .ExportAsync(Arg.Is<ReportExportDocument>(d => d.Rows.Count == 2), ReportExportFormat.Csv,
                "attendance-register-2026-08-15", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportAttendanceRegisterQuery(new DateOnly(2026, 8, 15), null, null, ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(Arg.Any<GetAttendanceRecordsForTenantQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Multi_Page_Result_Pages_Through_Until_Total_Reached_Without_Truncating()
    {
        AttendanceRegisterEntryDto[] page1Entries = Enumerable.Range(0, 100).Select(_ => Entry()).ToArray();
        AttendanceRegisterEntryDto[] page2Entries = Enumerable.Range(0, 30).Select(_ => Entry()).ToArray();

        PagedResult<AttendanceRegisterEntryDto> page1 = new(page1Entries, 1, 100, 130);
        PagedResult<AttendanceRegisterEntryDto> page2 = new(page2Entries, 2, 100, 130);

        _sender.Send(Arg.Is<GetAttendanceRecordsForTenantQuery>(q => q.Page == 1), Arg.Any<CancellationToken>()).Returns(page1);
        _sender.Send(Arg.Is<GetAttendanceRecordsForTenantQuery>(q => q.Page == 2), Arg.Any<CancellationToken>()).Returns(page2);

        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "attendance-register.csv"));

        await CreateHandler().Handle(
            new ExportAttendanceRegisterQuery(null, null, null, ReportExportFormat.Csv), CancellationToken.None);

        await _sender.Received(2).Send(Arg.Any<GetAttendanceRecordsForTenantQuery>(), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Rows.Count == 130),
            Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_And_Filters_Through_To_Underlying_Query()
    {
        Guid staffMemberId = Guid.NewGuid();
        PagedResult<AttendanceRegisterEntryDto> page1 = new([Entry()], 1, 100, 1);
        _sender.Send(Arg.Any<GetAttendanceRecordsForTenantQuery>(), Arg.Any<CancellationToken>()).Returns(page1);
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "attendance-register.pdf"));

        await CreateHandler().Handle(
            new ExportAttendanceRegisterQuery(new DateOnly(2026, 8, 15), staffMemberId, true, ReportExportFormat.Pdf),
            CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<GetAttendanceRecordsForTenantQuery>(q =>
                q.WorkDate == new DateOnly(2026, 8, 15) && q.StaffMemberId == staffMemberId && q.OnlyOpen == true),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
