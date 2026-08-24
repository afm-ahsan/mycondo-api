using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFixedDepositInterestReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFixedDepositInterestReport;

public class FixedDepositInterestExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all Fixed Deposits)", DateTimeOffset.UtcNow, Guid.NewGuid());

        FixedDepositInterestReportDto report = new(
            metadata,
            AccruedGrossForPeriod: 1_000m,
            LedgerInterestIncomeForPeriod: 1_000m,
            IsReconciled: true,
            ReceivedGrossForPeriod: 900m,
            ReceivedDeductionForPeriod: 100m,
            ReceivedNetForPeriod: 800m,
            OutstandingAccruedNotReceivedAsOfToDate: 200m,
            ByFixedDeposit: [new FixedDepositInterestLineDto(Guid.NewGuid(), "FD-001", 1_000m, 900m, 100m, 800m)]);

        ReportExportDocument document = FixedDepositInterestExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Fixed Deposit Interest");
        document.MetadataLines.Should().Contain(("From", "2026-08-01"));
        document.MetadataLines.Should().Contain(("To", "2026-08-31"));
        document.Columns.Select(c => c.Header).Should().Equal(
            "Certificate Number", "Accrued (Gross)", "Received (Gross)", "Received (Deduction)", "Received (Net)");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("FD-001", "1,000.00", "900.00", "100.00", "800.00");
        document.Totals.Should().Contain(("Accrued (Gross) for Period", "1,000.00"));
        document.Totals.Should().Contain(("Reconciled", "Yes"));
        document.Totals.Should().Contain(("Outstanding Accrued (Not Received) as of To Date", "200.00"));
    }
}
