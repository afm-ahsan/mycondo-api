using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFineReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFineReport;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFineReport;

public class FineReportExportMapperTests
{
    [Fact]
    public void Maps_Metadata_And_Metric_Rows()
    {
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (Fine invoices)", DateTimeOffset.UtcNow, Guid.NewGuid());

        FineReportDto report = new(metadata, 2_000m, 4, 1_000m, 500m);

        ReportExportDocument document = FineReportExportMapper.ToExportDocument(report);

        document.Title.Should().Be("Fines");
        document.Rows.Should().Contain(r => r[0] == "Billed" && r[1] == "2,000.00");
        document.Rows.Should().Contain(r => r[0] == "Billed Invoice Count" && r[1] == "4");
        document.Rows.Should().Contain(r => r[0] == "Collected" && r[1] == "1,000.00");
        document.Rows.Should().Contain(r => r[0] == "Waived" && r[1] == "500.00");
    }
}
