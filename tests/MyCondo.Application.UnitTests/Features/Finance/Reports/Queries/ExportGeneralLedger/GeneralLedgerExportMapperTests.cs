using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportGeneralLedger;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGeneralLedger;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportGeneralLedger;

public class GeneralLedgerExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Debit_Credit_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all accounts)", DateTimeOffset.UtcNow, Guid.NewGuid());

        GeneralLedgerLineDto debitLine = new(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 5), "Cash receipt", "Payment", Guid.NewGuid(),
            Guid.NewGuid(), "1010", "Cash", null, null, "Debit", 500m);
        GeneralLedgerLineDto creditLine = new(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 6), "Service charge posting", "Invoice", Guid.NewGuid(),
            Guid.NewGuid(), "4010", "Service Charge Income", null, null, "Credit", 500m);

        GeneralLedgerReportDto report = new(metadata, [debitLine, creditLine], 1, 2, 2);

        ReportExportDocument document = GeneralLedgerExportMapper.ToExportDocument(report);

        document.Title.Should().Be("General Ledger");
        document.Columns.Select(c => c.Header).Should()
            .Equal("Date", "Description", "Reference Type", "Account Code", "Account", "Direction", "Amount");
        document.Rows.Should().HaveCount(2);
        document.Rows[0].Should().Equal("2026-08-05", "Cash receipt", "Payment", "1010", "Cash", "Debit", "500.00");
        document.Totals.Should().Contain(("Total Debit", "500.00"));
        document.Totals.Should().Contain(("Total Credit", "500.00"));
    }
}
