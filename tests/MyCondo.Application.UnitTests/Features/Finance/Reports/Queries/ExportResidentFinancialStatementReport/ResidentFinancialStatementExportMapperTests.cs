using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportResidentFinancialStatementReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportResidentFinancialStatementReport;

public class ResidentFinancialStatementExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Flat A-101", DateTimeOffset.UtcNow, Guid.NewGuid());

        ResidentStatementLineDto line = new(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 5), "Service charge", "Invoice", Guid.NewGuid(), "Debit", 500m, 500m);

        ResidentFinancialStatementReportDto report = new(
            metadata, Guid.NewGuid(), "A-101", 0m, [line], 500m, 1, 200, 1);

        ReportExportDocument document = ResidentFinancialStatementExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Resident Financial Statement");
        document.MetadataLines.Should().Contain(("Flat", "A-101"));
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("2026-08-05", "Service charge", "Invoice", "Debit", "500.00", "500.00");
        document.Totals.Should().Contain(("Closing Balance", "500.00"));
    }
}
