using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportServiceChargeCollectionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportServiceChargeCollectionReport;

public class ServiceChargeCollectionExportMapperTests
{
    [Fact]
    public void Maps_Metadata_And_Metric_Rows()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (Service Charge invoices)", DateTimeOffset.UtcNow, Guid.NewGuid());

        ServiceChargeCollectionReportDto report = new(metadata, 20_000m, 10, 18_000m, 500m);

        ReportExportDocument document = ServiceChargeCollectionExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Service Charge Collection");
        document.Rows.Should().Contain(r => r[0] == "Billed" && r[1] == "20,000.00");
        document.Rows.Should().Contain(r => r[0] == "Billed Invoice Count" && r[1] == "10");
        document.Rows.Should().Contain(r => r[0] == "Collected" && r[1] == "18,000.00");
        document.Rows.Should().Contain(r => r[0] == "Waived" && r[1] == "500.00");
    }
}
