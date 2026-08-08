using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetReadingStatusSummaryReport;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Readings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetReadingStatusSummaryReport;

public class GetReadingStatusSummaryReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IReadingRepository _readings = Substitute.For<IReadingRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetReadingStatusSummaryReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetReadingStatusSummaryReportQueryHandler CreateHandler() => new(_readings, _currentUser);

    [Fact]
    public async Task Maps_Repository_Lines_To_Dtos_With_String_Fields()
    {
        List<ReadingStatusSummaryLine> lines =
        [
            new(UtilityType.Electricity, ReadingStatus.Finalized, 5),
            new(UtilityType.Electricity, ReadingStatus.Draft, 2),
        ];

        _readings.GetStatusSummaryAsync(TenantId, null, null, Arg.Any<CancellationToken>()).Returns(lines);

        IReadOnlyList<ReadingStatusSummaryLineDto> result = await CreateHandler().Handle(
            new GetReadingStatusSummaryReportQuery(null, null), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(l => l.UtilityType == "Electricity" && l.Status == "Finalized" && l.Count == 5);
        result.Should().ContainSingle(l => l.UtilityType == "Electricity" && l.Status == "Draft" && l.Count == 2);
    }

    [Fact]
    public async Task Building_And_UtilityType_Filters_Are_Converted_And_Passed_To_The_Repository()
    {
        Guid rawBuildingId = Guid.NewGuid();
        _readings.GetStatusSummaryAsync(TenantId, new BuildingId(rawBuildingId), UtilityType.Gas, Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new GetReadingStatusSummaryReportQuery(rawBuildingId, "Gas"), CancellationToken.None);

        await _readings.Received(1).GetStatusSummaryAsync(TenantId, new BuildingId(rawBuildingId), UtilityType.Gas, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetReadingStatusSummaryReportQuery(null, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
