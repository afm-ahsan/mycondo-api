using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseByTypeReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByTypeReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportExpenseByTypeReport;

public class ExpenseByTypeExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all expenses)", DateTimeOffset.UtcNow, Guid.NewGuid());

        ExpenseByTypeReportDto report = new(
            metadata,
            [new ExpenseByTypeLineDto(Guid.NewGuid(), "Electricity", Guid.NewGuid(), "Utilities", 3, 5_000m)],
            5_000m);

        ReportExportDocument document = ExpenseByTypeExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Expense by Type");
        document.MetadataLines.Should().Contain(("From", "2026-08-01"));
        document.MetadataLines.Should().Contain(("To", "2026-08-31"));
        document.Columns.Select(c => c.Header).Should().Equal("Type", "Category", "Count", "Amount");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("Electricity", "Utilities", "3", "5,000.00");
        document.Totals.Should().Contain(("Total", "5,000.00"));
    }
}
