using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseTrendReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportExpenseTrendReport;

public class ExpenseTrendExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31), "Tenant (all expenses)", DateTimeOffset.UtcNow, Guid.NewGuid());

        ExpenseTrendReportDto report = new(
            metadata,
            [new ExpenseTrendMonthDto(2026, 8, 5_000m)],
            5_000m);

        ReportExportDocument document = ExpenseTrendExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Expense Trend");
        document.MetadataLines.Should().Contain(("From", "2026-06-01"));
        document.MetadataLines.Should().Contain(("To", "2026-08-31"));
        document.Columns.Select(c => c.Header).Should().Equal("Month", "Amount");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("2026-08", "5,000.00");
        document.Totals.Should().Contain(("Total", "5,000.00"));
    }
}
