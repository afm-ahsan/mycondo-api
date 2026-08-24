using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseByCategoryReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportExpenseByCategoryReport;

public class ExpenseByCategoryExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all expenses)", DateTimeOffset.UtcNow, Guid.NewGuid());

        ExpenseByCategoryReportDto report = new(
            metadata,
            [new ExpenseByCategoryLineDto(Guid.NewGuid(), "Utilities", 5_000m)],
            5_000m);

        ReportExportDocument document = ExpenseByCategoryExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Expense by Category");
        document.MetadataLines.Should().Contain(("From", "2026-08-01"));
        document.MetadataLines.Should().Contain(("To", "2026-08-31"));
        document.MetadataLines.Should().Contain(("Scope", "Tenant (all expenses)"));
        document.Columns.Select(c => c.Header).Should().Equal("Category", "Amount");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("Utilities", "5,000.00");
        document.Totals.Should().Contain(("Total", "5,000.00"));
    }
}
