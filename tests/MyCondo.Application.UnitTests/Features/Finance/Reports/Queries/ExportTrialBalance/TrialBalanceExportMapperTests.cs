using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportTrialBalance;
using MyCondo.Application.Features.Finance.Reports.Queries.GetTrialBalance;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportTrialBalance;

public class TrialBalanceExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            new DateOnly(2026, 8, 15), "Tenant (all accounts)", DateTimeOffset.UtcNow, Guid.NewGuid());

        TrialBalanceReportDto report = new(
            metadata,
            [new TrialBalanceLineDto(Guid.NewGuid(), "1000", "Cash & Bank", "Asset", "Debit", 7_000m, 0m)],
            7_000m,
            7_000m);

        ReportExportDocument document = TrialBalanceExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Trial Balance");
        document.MetadataLines.Should().Contain(("As Of", "2026-08-15"));
        document.MetadataLines.Should().Contain(("Scope", "Tenant (all accounts)"));
        document.Columns.Select(c => c.Header).Should().Equal("Code", "Account", "Category", "Debit", "Credit");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("1000", "Cash & Bank", "Asset", "7,000.00", "0.00");
        document.Totals.Should().Contain(("Total Debit", "7,000.00"));
        document.Totals.Should().Contain(("Total Credit", "7,000.00"));
    }
}
