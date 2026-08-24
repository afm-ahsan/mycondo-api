using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFixedDepositPortfolioReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFixedDepositPortfolioReport;

public class FixedDepositPortfolioExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            new DateOnly(2026, 8, 15), "Tenant (all Fixed Deposits)", DateTimeOffset.UtcNow, Guid.NewGuid());

        FixedDepositPortfolioReportDto report = new(
            metadata,
            [new FixedDepositPortfolioLineDto(
                Guid.NewGuid(), "FD-001", "ABC Bank", "Main Branch", 100_000m, 6.5m,
                new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), "Active", 3_000m, 2_000m, 1_000m)],
            100_000m,
            1_000m,
            1);

        ReportExportDocument document = FixedDepositPortfolioExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Fixed Deposit Portfolio");
        document.MetadataLines.Should().Contain(("As Of", "2026-08-15"));
        document.Columns.Select(c => c.Header).Should().Equal(
            "Certificate Number", "Bank", "Branch", "Principal", "Rate (%)", "Start Date", "Maturity Date",
            "Status", "Accrued Interest", "Received Interest", "Outstanding Interest");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal(
            "FD-001", "ABC Bank", "Main Branch", "100,000.00", "6.50", "2026-01-01", "2027-01-01",
            "Active", "3,000.00", "2,000.00", "1,000.00");
        document.Totals.Should().Contain(("Total Principal", "100,000.00"));
        document.Totals.Should().Contain(("Total Outstanding Accrued Interest", "1,000.00"));
        document.Totals.Should().Contain(("Active Count", "1"));
    }

    [Fact]
    public void Maps_Null_Branch_To_Empty_String()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            new DateOnly(2026, 8, 15), "Tenant (all Fixed Deposits)", DateTimeOffset.UtcNow, null);

        FixedDepositPortfolioReportDto report = new(
            metadata,
            [new FixedDepositPortfolioLineDto(
                Guid.NewGuid(), "FD-002", "XYZ Bank", null, 50_000m, 5m,
                new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), "Active", 0m, 0m, 0m)],
            50_000m,
            0m,
            1);

        ReportExportDocument document = FixedDepositPortfolioExportMapper.ToExportDocument(report);

        document.Rows[0][2].Should().Be(string.Empty);
    }
}
