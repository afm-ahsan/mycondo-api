using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportCashFlow;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashFlow;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportCashFlow;

public class CashFlowExportMapperTests
{
    [Fact]
    public void Maps_Metadata_And_Metric_Rows()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all cash/bank accounts)", DateTimeOffset.UtcNow, Guid.NewGuid());

        CashFlowReportDto report = new(
            metadata, 10_000m, 5_000m, 2_000m, 3_000m, 1_000m, 500m, 500m, 3_500m, 13_500m);

        ReportExportDocument document = CashFlowExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Cash Flow");
        document.MetadataLines.Should().Contain(("From", "2026-08-01"));
        document.MetadataLines.Should().Contain(("To", "2026-08-31"));
        document.Columns.Select(c => c.Header).Should().Equal("Metric", "Amount");
        document.Rows.Should().Contain(r => r[0] == "Opening Cash Balance" && r[1] == "10,000.00");
        document.Rows.Should().Contain(r => r[0] == "Closing Cash Balance" && r[1] == "13,500.00");
    }
}
