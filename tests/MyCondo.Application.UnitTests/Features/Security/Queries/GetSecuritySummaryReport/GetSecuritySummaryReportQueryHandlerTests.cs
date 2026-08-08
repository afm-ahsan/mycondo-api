using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.DTOs;
using MyCondo.Application.Features.Security.Queries.GetSecuritySummaryReport;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.Parcels;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Security.Queries.GetSecuritySummaryReport;

public class GetSecuritySummaryReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IAccessSessionRepository _accessSessions = Substitute.For<IAccessSessionRepository>();
    private readonly IParcelRepository _parcels = Substitute.For<IParcelRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetSecuritySummaryReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetSecuritySummaryReportQueryHandler CreateHandler() => new(_accessSessions, _parcels, _currentUser);

    [Fact]
    public async Task Combines_Currently_Inside_Counts_And_Awaiting_Collection_Into_The_Dto()
    {
        List<CurrentlyInsideCategoryCount> counts =
        [
            new(AccessCategory.Guest, 5),
            new(AccessCategory.Vehicle, 3),
        ];
        _accessSessions.GetCurrentlyInsideCountsByCategoryAsync(TenantId, Arg.Any<CancellationToken>()).Returns(counts);
        _parcels.GetAwaitingCollectionCountAsync(TenantId, Arg.Any<CancellationToken>()).Returns(7);

        SecuritySummaryDto result = await CreateHandler().Handle(new GetSecuritySummaryReportQuery(), CancellationToken.None);

        result.CurrentlyInside.Should().HaveCount(2);
        result.CurrentlyInside.Should().ContainSingle(c => c.Category == "Guest" && c.Count == 5);
        result.CurrentlyInside.Should().ContainSingle(c => c.Category == "Vehicle" && c.Count == 3);
        result.ParcelsAwaitingCollectionCount.Should().Be(7);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetSecuritySummaryReportQuery(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
