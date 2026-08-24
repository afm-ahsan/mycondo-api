using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialPosition;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFinancialPosition;

public class FinancialPositionExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Sections_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            new DateOnly(2026, 8, 15), "Tenant (all accounts)", DateTimeOffset.UtcNow, Guid.NewGuid());

        FinancialPositionReportDto report = new(
            metadata,
            new FinancialPositionSectionDto([new FinancialPositionLineDto(Guid.NewGuid(), "1010", "Cash", 10_000m)], 10_000m),
            new FinancialPositionSectionDto([new FinancialPositionLineDto(Guid.NewGuid(), "2010", "Payable", 3_000m)], 3_000m),
            new FinancialPositionSectionDto([new FinancialPositionLineDto(Guid.NewGuid(), "3010", "Reserve", 5_000m)], 5_000m),
            2_000m,
            10_000m);

        ReportExportDocument document = FinancialPositionExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Financial Position");
        document.MetadataLines.Should().Contain(("As Of", "2026-08-15"));
        document.Columns.Select(c => c.Header).Should().Equal("Section", "Code", "Account", "Balance");
        document.Rows.Should().HaveCount(3);
        document.Rows[0].Should().Equal("Assets", "1010", "Cash", "10,000.00");
        document.Rows[1].Should().Equal("Liabilities", "2010", "Payable", "3,000.00");
        document.Rows[2].Should().Equal("Equity", "3010", "Reserve", "5,000.00");
        document.Totals.Should().Contain(("Total Assets", "10,000.00"));
        document.Totals.Should().Contain(("Total Liabilities & Equity", "10,000.00"));
    }
}
