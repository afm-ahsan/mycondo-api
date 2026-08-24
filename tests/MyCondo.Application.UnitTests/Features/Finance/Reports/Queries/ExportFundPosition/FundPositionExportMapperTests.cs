using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFundPosition;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFundPosition;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFundPosition;

public class FundPositionExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            new DateOnly(2026, 8, 15), "Tenant (all funds)", DateTimeOffset.UtcNow, Guid.NewGuid());

        FundPositionReportDto report = new(
            metadata,
            [new FundPositionLineDto(Guid.NewGuid(), "SNK", "Sinking Fund", 25_000m)],
            25_000m);

        ReportExportDocument document = FundPositionExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Fund Position");
        document.MetadataLines.Should().Contain(("As Of", "2026-08-15"));
        document.MetadataLines.Should().Contain(("Scope", "Tenant (all funds)"));
        document.Columns.Select(c => c.Header).Should().Equal("Code", "Fund", "Balance");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("SNK", "Sinking Fund", "25,000.00");
        document.Totals.Should().Contain(("Total Balance", "25,000.00"));
    }
}
