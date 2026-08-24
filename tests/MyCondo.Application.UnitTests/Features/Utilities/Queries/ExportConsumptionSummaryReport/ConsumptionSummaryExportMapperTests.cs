using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.ExportConsumptionSummaryReport;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.ExportConsumptionSummaryReport;

public class ConsumptionSummaryExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_And_Rows()
    {
        IReadOnlyList<ConsumptionSummaryLineDto> lines =
        [
            new ConsumptionSummaryLineDto("Electricity", 1_250.5m, 10),
            new ConsumptionSummaryLineDto("Gas", 300m, 4),
        ];

        ReportExportDocument document = ConsumptionSummaryExportMapper.ToExportDocument(
            lines, null, null, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        document.Title.Should().Be("Consumption Summary");
        document.MetadataLines.Should().Contain(("From", "2026-08-01"));
        document.MetadataLines.Should().Contain(("To", "2026-08-31"));
        document.MetadataLines.Should().Contain(("Building", "All buildings"));
        document.MetadataLines.Should().Contain(("Utility Type", "All"));
        document.Columns.Select(c => c.Header).Should().Equal("Utility Type", "Total Consumption (Units)", "Reading Count");
        document.Rows.Should().HaveCount(2);
        document.Rows[0].Should().Equal("Electricity", "1,250.50", "10");
        document.Rows[1].Should().Equal("Gas", "300.00", "4");
    }

    [Fact]
    public void Includes_Building_And_Utility_Type_Filters_When_Provided()
    {
        Guid buildingId = Guid.NewGuid();

        ReportExportDocument document = ConsumptionSummaryExportMapper.ToExportDocument(
            [], buildingId, "Gas", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        document.MetadataLines.Should().Contain(("Building", buildingId.ToString()));
        document.MetadataLines.Should().Contain(("Utility Type", "Gas"));
    }
}
