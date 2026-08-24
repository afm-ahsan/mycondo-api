using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportCashBankPositionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashBankPositionReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportCashBankPositionReport;

public class CashBankPositionExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            new DateOnly(2026, 8, 15), "Tenant (all financial accounts)", DateTimeOffset.UtcNow, Guid.NewGuid());

        CashBankPositionReportDto report = new(
            metadata,
            [new CashBankAccountLineDto(Guid.NewGuid(), "Main Bank", "Bank", "ABC Bank", "Downtown", "12345", 15_000m)],
            15_000m);

        ReportExportDocument document = CashBankPositionExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Cash & Bank Position");
        document.Columns.Select(c => c.Header).Should().Equal("Account", "Type", "Bank", "Branch", "Account Number", "Balance");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("Main Bank", "Bank", "ABC Bank", "Downtown", "12345", "15,000.00");
        document.Totals.Should().Contain(("Total Balance", "15,000.00"));
    }
}
