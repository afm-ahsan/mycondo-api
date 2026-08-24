using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialOverview;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFinancialOverview;

public class FinancialOverviewExportMapperTests
{
    [Fact]
    public void Maps_Metadata_ExpenseComposition_Rows_And_Totals()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            new DateOnly(2026, 8, 15), "Tenant (all accounts)", DateTimeOffset.UtcNow, Guid.NewGuid());

        FinancialOverviewReportDto report = new(
            metadata, 50_000m, 30_000m, 20_000m, 10_000m,
            new CashPositionDto(1_000m, 5_000m, 500m, 6_500m),
            25_000m,
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 15),
            new CollectionPerformanceDto(12_000m, 10_000m),
            [new ExpenseCompositionLineDto(Guid.NewGuid(), "Utilities", 30_000m, 100m)]);

        ReportExportDocument document = FinancialOverviewExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Financial Overview");
        document.MetadataLines.Should().Contain(("As Of", "2026-08-15"));
        document.MetadataLines.Should().Contain(("Collection Period From", "2026-08-01"));
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("Utilities", "30,000.00", "100.00");
        document.Totals.Should().Contain(("Income Total", "50,000.00"));
        document.Totals.Should().Contain(("Surplus/Deficit", "20,000.00"));
        document.Totals.Should().Contain(("Available Liquid Funds", "6,500.00"));
    }
}
