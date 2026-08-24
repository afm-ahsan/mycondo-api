using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportIncomeExpenseReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportIncomeExpenseReport;

public class IncomeExpenseExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Income_Expense_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all accounts)", DateTimeOffset.UtcNow, Guid.NewGuid());

        IncomeExpenseReportDto report = new(
            metadata,
            [new IncomeExpenseLineDto(Guid.NewGuid(), "4010", "Service Charge Income", 20_000m)],
            20_000m,
            [new IncomeExpenseLineDto(Guid.NewGuid(), "5010", "Utilities Expense", 8_000m)],
            8_000m,
            12_000m);

        ReportExportDocument document = IncomeExpenseExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Income & Expense");
        document.Columns.Select(c => c.Header).Should().Equal("Section", "Code", "Account", "Amount");
        document.Rows.Should().HaveCount(2);
        document.Rows[0].Should().Equal("Income", "4010", "Service Charge Income", "20,000.00");
        document.Rows[1].Should().Equal("Expense", "5010", "Utilities Expense", "8,000.00");
        document.Totals.Should().Contain(("Surplus/Deficit", "12,000.00"));
    }
}
