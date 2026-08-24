using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseSummaryReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseSummaryReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportExpenseSummaryReport;

public class ExpenseSummaryExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all expenses)", DateTimeOffset.UtcNow, Guid.NewGuid());

        ExpenseSummaryReportDto report = new(
            metadata, 15_000m, 15_000m, true,
            [new ExpenseStatusBreakdownDto("Posted", 6, 15_000m)]);

        ReportExportDocument document = ExpenseSummaryExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Expense Summary");
        document.Columns.Select(c => c.Header).Should().Equal("Status", "Count", "Total Amount");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("Posted", "6", "15,000.00");
        document.Totals.Should().Contain(("Ledger Total", "15,000.00"));
        document.Totals.Should().Contain(("Reconciled", "Yes"));
    }
}
