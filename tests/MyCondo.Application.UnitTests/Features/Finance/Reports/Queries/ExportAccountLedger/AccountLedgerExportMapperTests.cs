using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportAccountLedger;
using MyCondo.Application.Features.Finance.Reports.Queries.GetAccountLedger;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportAccountLedger;

public class AccountLedgerExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Account 1010", DateTimeOffset.UtcNow, Guid.NewGuid());

        AccountLedgerLineDto line = new(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 5), "Cash receipt", "Payment", Guid.NewGuid(), null, "Debit", 500m, 500m);

        AccountLedgerReportDto report = new(
            metadata, Guid.NewGuid(), "1010", "Cash", "Debit", 0m, [line], 500m, 1, 200, 1);

        ReportExportDocument document = AccountLedgerExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Account Ledger");
        document.MetadataLines.Should().Contain(("Account", "1010 - Cash"));
        document.Columns.Select(c => c.Header).Should()
            .Equal("Date", "Description", "Reference Type", "Direction", "Amount", "Running Balance");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("2026-08-05", "Cash receipt", "Payment", "Debit", "500.00", "500.00");
        document.Totals.Should().Contain(("Opening Balance", "0.00"));
        document.Totals.Should().Contain(("Closing Balance", "500.00"));
    }
}
