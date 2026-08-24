using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.ExportFinancialSummaryReport;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.ExportFinancialSummaryReport;

public class FinancialSummaryExportMapperTests
{
    [Fact]
    public void Maps_Metadata_And_Metric_Rows()
    {
        FinancialSummaryDto report = new(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 8, 23),
            10_000m,
            7_500m,
            2_500m,
            3,
            1,
            2);

        ReportExportDocument document = FinancialSummaryExportMapper.ToExportDocument(report, buildingName: null);

        document.Title.Should().Be("Financial Summary");
        document.MetadataLines.Should().Contain(("Period", "2026-08-01 to 2026-08-31"));
        document.MetadataLines.Should().Contain(("As Of", "2026-08-23"));
        document.MetadataLines.Should().Contain(("Scope", "All Buildings"));
        document.Columns.Select(c => c.Header).Should().Equal("Metric", "Value");
        document.Rows.Should().Contain(row => row[0] == "Total Billed" && row[1] == "10,000.00");
        document.Rows.Should().Contain(row => row[0] == "Total Collected" && row[1] == "7,500.00");
        document.Rows.Should().Contain(row => row[0] == "Total Outstanding" && row[1] == "2,500.00");
        document.Rows.Should().Contain(row => row[0] == "Unpaid Invoices" && row[1] == "3");
        document.Rows.Should().Contain(row => row[0] == "Partially Paid Invoices" && row[1] == "1");
        document.Rows.Should().Contain(row => row[0] == "Overdue Invoices" && row[1] == "2");
    }

    [Fact]
    public void Scopes_Metadata_To_Building_Name_When_Provided()
    {
        FinancialSummaryDto report = new(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 23),
            0m, 0m, 0m, 0, 0, 0);

        ReportExportDocument document = FinancialSummaryExportMapper.ToExportDocument(report, buildingName: "Lakeview Towers");

        document.MetadataLines.Should().Contain(("Scope", "Lakeview Towers"));
    }
}
