using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.ExportReadingStatusSummaryReport;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.ExportReadingStatusSummaryReport;

public class ReadingStatusSummaryExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        IReadOnlyList<ReadingStatusSummaryLineDto> lines =
        [
            new ReadingStatusSummaryLineDto("Electricity", "Billed", 5),
            new ReadingStatusSummaryLineDto("Electricity", "Draft", 2),
        ];

        ReportExportDocument document = ReadingStatusSummaryExportMapper.ToExportDocument(lines, null, "Electricity");

        document.Title.Should().Be("Reading Status Summary");
        document.MetadataLines.Should().Contain(("Building", "All buildings"));
        document.MetadataLines.Should().Contain(("Utility Type", "Electricity"));
        document.Columns.Select(c => c.Header).Should().Equal("Utility Type", "Status", "Count");
        document.Rows.Should().HaveCount(2);
        document.Rows[0].Should().Equal("Electricity", "Billed", "5");
        document.Rows[1].Should().Equal("Electricity", "Draft", "2");
        document.Totals.Should().Contain(("Total Readings", "7"));
    }
}
