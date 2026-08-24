using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportGasCollectionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGasCollectionReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportGasCollectionReport;

public class GasCollectionExportMapperTests
{
    [Fact]
    public void Maps_Metadata_And_Metric_Rows()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (Gas recovery invoices)", DateTimeOffset.UtcNow, Guid.NewGuid());

        GasCollectionReportDto report = new(metadata, 6_000m, 5, 5_500m, 0m);

        ReportExportDocument document = GasCollectionExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Gas Collection");
        document.Rows.Should().Contain(r => r[0] == "Billed" && r[1] == "6,000.00");
        document.Rows.Should().Contain(r => r[0] == "Collected" && r[1] == "5,500.00");
        document.Rows.Should().Contain(r => r[0] == "Waived" && r[1] == "0.00");
    }
}
