using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.Queries.GetRatePlans;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetRatePlans;

public class GetRatePlansQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = new(Guid.NewGuid());

    private readonly IRatePlanRepository _ratePlans = Substitute.For<IRatePlanRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetRatePlansQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _ratePlans.SearchAsync(Arg.Any<Guid>(), Arg.Any<BuildingId?>(), Arg.Any<UtilityType?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<RatePlan>([], 1, 20, 0));
    }

    private GetRatePlansQueryHandler CreateHandler() => new(_ratePlans, _currentUser);

    [Fact]
    public async Task Passes_Null_BuildingId_When_Omitted()
    {
        await CreateHandler().Handle(new GetRatePlansQuery(null, null, 1, 20), CancellationToken.None);

        await _ratePlans.Received(1).SearchAsync(TenantId, null, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_BuildingId_When_Given()
    {
        await CreateHandler().Handle(new GetRatePlansQuery(BuildingId.Value, null, 1, 20), CancellationToken.None);

        await _ratePlans.Received(1).SearchAsync(TenantId, BuildingId, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Unauthenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetRatePlansQuery(null, null, 1, 20), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
