using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportOutstandingDuesReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetOutstandingDuesReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportOutstandingDuesReport;

public class OutstandingDuesExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            new DateOnly(2026, 8, 15), "Tenant (all buildings)", DateTimeOffset.UtcNow, Guid.NewGuid());

        OutstandingDuesReportDto report = new(
            metadata,
            [new OutstandingDuesLineDto(Guid.NewGuid(), "A-101", Guid.NewGuid(), 3_500m, 2)],
            3_500m);

        ReportExportDocument document = OutstandingDuesExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Outstanding Dues");
        document.Columns.Select(c => c.Header).Should().Equal("Flat", "Outstanding Balance", "Open Invoices");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("A-101", "3,500.00", "2");
        document.Totals.Should().Contain(("Total Outstanding", "3,500.00"));
    }
}
