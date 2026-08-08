using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetMeterStatusSummaryReport;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetMeterStatusSummaryReport;

public class GetMeterStatusSummaryReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IMeterRepository _meters = Substitute.For<IMeterRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetMeterStatusSummaryReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetMeterStatusSummaryReportQueryHandler CreateHandler() => new(_meters, _currentUser);

    [Fact]
    public async Task Maps_Repository_Lines_To_Dtos_With_String_Fields()
    {
        List<MeterStatusSummaryLine> lines =
        [
            new(UtilityType.Electricity, MeterStatus.Active, 40),
            new(UtilityType.Electricity, MeterStatus.Faulty, 3),
        ];

        _meters.GetStatusSummaryAsync(TenantId, null, null, Arg.Any<CancellationToken>()).Returns(lines);

        IReadOnlyList<MeterStatusSummaryLineDto> result = await CreateHandler().Handle(
            new GetMeterStatusSummaryReportQuery(null, null), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(l => l.UtilityType == "Electricity" && l.Status == "Active" && l.Count == 40);
        result.Should().ContainSingle(l => l.UtilityType == "Electricity" && l.Status == "Faulty" && l.Count == 3);
    }

    [Fact]
    public async Task Building_And_UtilityType_Filters_Are_Converted_And_Passed_To_The_Repository()
    {
        Guid rawBuildingId = Guid.NewGuid();
        _meters.GetStatusSummaryAsync(TenantId, new BuildingId(rawBuildingId), UtilityType.Gas, Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new GetMeterStatusSummaryReportQuery(rawBuildingId, "Gas"), CancellationToken.None);

        await _meters.Received(1).GetStatusSummaryAsync(TenantId, new BuildingId(rawBuildingId), UtilityType.Gas, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetMeterStatusSummaryReportQuery(null, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
