using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetConsumptionSummaryReport;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Readings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetConsumptionSummaryReport;

public class GetConsumptionSummaryReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IReadingRepository _readings = Substitute.For<IReadingRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetConsumptionSummaryReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetConsumptionSummaryReportQueryHandler CreateHandler() => new(_readings, _currentUser);

    [Fact]
    public async Task Maps_Repository_Lines_To_Dtos_With_String_UtilityType()
    {
        DateOnly fromDate = new(2026, 8, 1);
        DateOnly toDate = new(2026, 8, 31);
        List<ConsumptionSummaryLine> lines =
        [
            new(UtilityType.Electricity, 4200m, 12),
            new(UtilityType.Gas, 800m, 8),
        ];

        _readings.GetConsumptionSummaryAsync(TenantId, null, null, fromDate, toDate, Arg.Any<CancellationToken>()).Returns(lines);

        IReadOnlyList<ConsumptionSummaryLineDto> result = await CreateHandler().Handle(
            new GetConsumptionSummaryReportQuery(null, null, fromDate, toDate), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(l => l.UtilityType == "Electricity" && l.TotalConsumptionUnits == 4200m && l.ReadingCount == 12);
        result.Should().ContainSingle(l => l.UtilityType == "Gas" && l.TotalConsumptionUnits == 800m && l.ReadingCount == 8);
    }

    [Fact]
    public async Task Building_And_UtilityType_Filters_Are_Converted_And_Passed_To_The_Repository()
    {
        Guid rawBuildingId = Guid.NewGuid();
        DateOnly fromDate = new(2026, 8, 1);
        DateOnly toDate = new(2026, 8, 31);
        _readings.GetConsumptionSummaryAsync(
                TenantId, new BuildingId(rawBuildingId), UtilityType.Gas, fromDate, toDate, Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new GetConsumptionSummaryReportQuery(rawBuildingId, "Gas", fromDate, toDate), CancellationToken.None);

        await _readings.Received(1).GetConsumptionSummaryAsync(
            TenantId, new BuildingId(rawBuildingId), UtilityType.Gas, fromDate, toDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetConsumptionSummaryReportQuery(null, null, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
