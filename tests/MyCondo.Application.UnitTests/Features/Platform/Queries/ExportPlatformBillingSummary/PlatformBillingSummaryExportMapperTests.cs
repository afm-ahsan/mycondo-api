using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.ExportPlatformBillingSummary;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.ExportPlatformBillingSummary;

public class PlatformBillingSummaryExportMapperTests
{
    [Fact]
    public void Maps_Metadata_And_One_Row_Per_Currency()
    {
        List<PlatformBillingSummaryCurrencyDto> summary =
        [
            new("BDT", 8000m, 2, 2000m, 1, 6000m, 2, 5000m, 1, 15),
            new("USD", 100m, 1, 0m, 0, 0m, 0, 0m, 0, null),
        ];

        ReportExportDocument document = PlatformBillingSummaryExportMapper.ToExportDocument(
            summary, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        document.Title.Should().Be("Platform Subscription Billing Summary");
        document.MetadataLines.Should().Contain(("From", "2026-08-01"));
        document.MetadataLines.Should().Contain(("To", "2026-08-31"));
        document.Columns.Select(c => c.Header).Should().Contain(["Currency", "Invoiced Amount", "Collected Amount", "Outstanding Amount"]);
        document.Rows.Should().Contain(r => r[0] == "BDT" && r[1] == "8,000.00" && r[9] == "15");
        document.Rows.Should().Contain(r => r[0] == "USD" && r[9] == string.Empty); // no overdue invoices — never a fabricated "0"
    }
}
