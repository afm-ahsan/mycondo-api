using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.ExportConsumptionHistoryReport;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.ExportConsumptionHistoryReport;

public class ConsumptionHistoryExportMapperTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ReadingDto Reading(DateOnly periodStart, decimal consumption, bool isAbnormal, string status) => new(
        ReadingId: Guid.NewGuid(),
        MeterId: Guid.NewGuid(),
        FlatId: Guid.NewGuid(),
        UtilityType: "Electricity",
        BuildingId: Guid.NewGuid(),
        PeriodStart: periodStart,
        PeriodEnd: periodStart.AddMonths(1),
        PreviousReading: 0m,
        PresentReading: consumption,
        ConsumptionUnits: consumption,
        ReadingDate: periodStart.AddMonths(1),
        OverrideReason: null,
        IsAbnormalConsumption: isAbnormal,
        AbnormalConsumptionReason: null,
        Status: status,
        ReviewedAtUtc: null,
        ReviewedBy: null,
        FinalizedAtUtc: null,
        FinalizedBy: null,
        BilledAtUtc: null,
        BilledBy: null,
        InvoiceId: null,
        CorrectsReadingId: null);

    [Fact]
    public void Maps_Metadata_Columns_And_Rows_Sorted_By_Period()
    {
        BuildingId buildingId = BuildingId.New();
        Meter meter = Meter.Install(TenantId, buildingId, UtilityType.Electricity, "MTR-001", DateTimeOffset.UtcNow);
        Building building = Building.Create(TenantId, "Aisha Tower", "AISHA", null, DateTimeOffset.UtcNow);

        IReadOnlyList<ReadingDto> readings =
        [
            Reading(new DateOnly(2026, 2, 1), 60m, isAbnormal: true, status: "Billed"),
            Reading(new DateOnly(2026, 1, 1), 50m, isAbnormal: false, status: "Finalized"),
        ];

        ReportExportDocument document = ConsumptionHistoryExportMapper.ToExportDocument(readings, meter, building);

        document.Title.Should().Be("Electricity Consumption History");
        document.MetadataLines.Should().Contain(("Building", "Aisha Tower"));
        document.MetadataLines.Should().Contain(("Meter", "MTR-001"));
        document.MetadataLines.Should().Contain(("Utility Type", "Electricity"));
        document.Columns.Select(c => c.Header).Should().Equal(
            "Period Start", "Period End", "Consumption (Units)", "Status", "Abnormal");
        document.Rows.Should().HaveCount(2);
        document.Rows[0].Should().Equal("2026-01-01", "2026-02-01", "50.00", "Finalized", "No");
        document.Rows[1].Should().Equal("2026-02-01", "2026-03-01", "60.00", "Billed", "Yes");
    }

    [Fact]
    public void Falls_Back_To_BuildingId_When_Building_Not_Found()
    {
        BuildingId buildingId = BuildingId.New();
        Meter meter = Meter.Install(TenantId, buildingId, UtilityType.Gas, "MTR-002", DateTimeOffset.UtcNow);

        ReportExportDocument document = ConsumptionHistoryExportMapper.ToExportDocument([], meter, building: null);

        document.MetadataLines.Should().Contain(("Building", buildingId.Value.ToString()));
    }
}
