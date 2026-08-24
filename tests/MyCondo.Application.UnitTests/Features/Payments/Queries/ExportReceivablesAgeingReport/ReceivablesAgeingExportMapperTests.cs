using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.ExportReceivablesAgeingReport;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.ExportReceivablesAgeingReport;

public class ReceivablesAgeingExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        ReceivablesAgeingReportDto report = new(
            new DateOnly(2026, 8, 15),
            [
                new AgeingBucketDto("Current", null, 0, 2, 5_000m),
                new AgeingBucketDto("1-30 days", 1, 30, 1, 1_200m),
            ],
            6_200m);

        ReportExportDocument document = ReceivablesAgeingExportMapper.ToExportDocument(report, buildingName: null);

        document.Title.Should().Be("Receivables Ageing");
        document.MetadataLines.Should().Contain(("As Of", "2026-08-15"));
        document.MetadataLines.Should().Contain(("Scope", "All Buildings"));
        document.Columns.Select(c => c.Header).Should().Equal("Ageing Bucket", "Invoice Count", "Balance");
        document.Rows.Should().HaveCount(2);
        document.Rows[0].Should().Equal("Current", "2", "5,000.00");
        document.Rows[1].Should().Equal("1-30 days", "1", "1,200.00");
        document.Totals.Should().Contain(("Grand Total", "6,200.00"));
    }

    [Fact]
    public void Scopes_Metadata_To_Building_Name_When_Provided()
    {
        ReceivablesAgeingReportDto report = new(new DateOnly(2026, 8, 15), [], 0m);

        ReportExportDocument document = ReceivablesAgeingExportMapper.ToExportDocument(report, buildingName: "Lakeview Towers");

        document.MetadataLines.Should().Contain(("Scope", "Lakeview Towers"));
    }
}
